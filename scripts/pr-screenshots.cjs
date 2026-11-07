const uiPath = /^src\/OpenWish\.Web(?:\.Client)?\/.+\.(?:razor(?:\.cs)?|cshtml(?:\.cs)?|css|scss|js|mjs|ts|tsx|resx|svg|png|jpe?g|webp|gif|woff2?)$/i;
const hostedImage = /!\[[^\]\r\n]+\]\(https:\/\/github\.com\/user-attachments\/assets\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}(?:\s+"[^"\r\n]*")?\)/i;

function validateScreenshots(files, body) {
  const committedAssets = files.filter(file =>
    file.status !== 'removed' && file.filename.startsWith('pr-assets/'));
  if (committedAssets.length) {
    return {
      passed: false,
      message: 'Do not commit pr-assets/. Upload review images as GitHub user attachments; see .docs/SCREENSHOTS.md.'
    };
  }

  const visibleBody = body.replace(/<!--[\s\S]*?-->/g, '')
    .replace(/```[\s\S]*?```/g, '').replace(/~~~[\s\S]*?~~~/g, '');
  const uiFiles = files.filter(file =>
    uiPath.test(file.filename) || uiPath.test(file.previous_filename || ''));
  const declaredUiChange = /^[ \t]*-[ \t]*UI impact:[ \t]*yes\b/im.test(visibleBody);
  if (!uiFiles.length && !declaredUiChange) {
    return { passed: true, message: 'No UI files or declared UI impact; screenshots are not required.' };
  }

  const missing = ['Desktop', 'Mobile'].filter(viewport => {
    const field = visibleBody.match(new RegExp(`^[ \\t]*-[ \\t]*${viewport} screenshot:[ \\t]*([^\\r\\n]*)`, 'im'));
    return !field || !hostedImage.test(field[1]);
  });
  if (missing.length) {
    return {
      passed: false,
      message: `UI changes require embedded GitHub user-attachment images with alt text in these PR fields: ${missing.join(', ')} screenshot. An artifact link, local path, or "N/A" is not an image. See .docs/SCREENSHOTS.md.`
    };
  }
  return {
    passed: true,
    message: 'Hosted desktop and mobile screenshot references are present. Reviewers must confirm that they show the affected journey on the latest revision.'
  };
}

module.exports = { validateScreenshots };
