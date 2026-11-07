const assert = require('node:assert/strict');
const { test } = require('node:test');
const { validateScreenshots } = require('./pr-screenshots.cjs');

const image = '![Wishlist on desktop](https://github.com/user-attachments/assets/12345678-1234-1234-1234-123456789abc)';
const body = `- Desktop screenshot: ${image}\n- Mobile screenshot: ${image}`;
const uiFile = { filename: 'src/OpenWish.Web.Client/Components/Wishlist/WishlistForm.razor', status: 'modified' };

test('UI changes require both hosted images, even when declared non-UI', () => {
  assert.equal(validateScreenshots([uiFile], '- UI impact: no').passed, false);
  assert.equal(validateScreenshots([uiFile], `- Desktop screenshot: ${image}`).passed, false);
  assert.equal(validateScreenshots([uiFile], `- Desktop screenshot:\n- Mobile screenshot: ${image}`).passed, false);
  assert.equal(validateScreenshots([uiFile], body).passed, true);
});

test('detects server/client markup, styles, scripts, copy, and visual assets', () => {
  for (const project of ['OpenWish.Web', 'OpenWish.Web.Client']) {
    for (const extension of ['razor', 'razor.cs', 'cshtml', 'cshtml.cs', 'css', 'scss', 'js', 'mjs', 'ts', 'tsx', 'resx', 'svg', 'png', 'jpg', 'jpeg', 'webp', 'gif', 'woff', 'woff2']) {
      assert.equal(validateScreenshots([{ filename: `src/${project}/Components/Test.${extension}` }], '').passed, false, `${project}: ${extension}`);
    }
  }
});

test('deleting or renaming UI files still needs evidence', () => {
  assert.equal(validateScreenshots([{ ...uiFile, status: 'removed' }], '').passed, false);
  assert.equal(validateScreenshots([{ filename: 'archive/form.txt', previous_filename: uiFile.filename, status: 'renamed' }], '').passed, false);
});

test('backend and documentation changes do not require screenshots unless declared UI-impacting', () => {
  const files = [{ filename: 'src/OpenWish.Application/Services/WishlistService.cs' }, { filename: '.docs/SCREENSHOTS.md' }];
  assert.equal(validateScreenshots(files, '').passed, true);
  assert.equal(validateScreenshots(files, '- UI impact: yes - changed the empty state').passed, false);
  assert.equal(validateScreenshots(files, `- UI impact: yes\n${body}`).passed, true);
});

test('rejects links, local paths, untrusted hosts, placeholders, and images without alt text', () => {
  for (const invalid of [
    'N/A', '[artifact](https://github.com/actions/runs/1)',
    '![Desktop](pr-assets/desktop.png)',
    '![Desktop](https://example.com/desktop.png)',
    '![Desktop](https://github.com.evil.test/user-attachments/assets/12345678-1234-1234-1234-123456789abc)',
    '![Desktop](https://github.com/user-attachments/assets/placeholder)',
    '![](https://github.com/user-attachments/assets/12345678-1234-1234-1234-123456789abc)'
  ]) {
    assert.equal(validateScreenshots([uiFile], `- Desktop screenshot: ${invalid}\n- Mobile screenshot: ${image}`).passed, false, invalid);
  }
});

test('template comments and fenced examples do not count as evidence or UI declarations', () => {
  for (const hidden of [`<!--\n${body}\n-->`, `\`\`\`markdown\n${body}\n\`\`\``, `~~~\n${body}\n~~~`]) {
    assert.equal(validateScreenshots([uiFile], hidden).passed, false);
  }
  assert.equal(validateScreenshots([], '<!-- - UI impact: yes -->').passed, true);
});

test('rejects force-added transient assets but permits their removal', () => {
  assert.equal(validateScreenshots([{ filename: 'pr-assets/desktop.png', status: 'added' }], body).passed, false);
  assert.equal(validateScreenshots([{ filename: 'pr-assets/desktop.png', status: 'removed' }], '').passed, true);
});
