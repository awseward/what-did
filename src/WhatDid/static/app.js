console.log('hello');

function isMissing(str) {
    return str == null || str.trim() === '';
}

function updateCompareUrl() {
    let anchor = document.getElementById('form-compare-link');
    if (anchor == null) { return; }

    let comparePath = tryBuildComparePath();

    anchor.setAttribute('href', comparePath || '');
    anchor.innerHTML = comparePath == null
        ? ""
        : "Let's go!";
}

function tryBuildComparePath() {
    let owner = document.getElementsByName('owner')[0].value;
    let repo = document.getElementsByName('repo')[0].value;
    let base = document.getElementsByName('base')[0].value;

    if (isMissing(owner) || isMissing(repo) || isMissing(base)) { return null; }

    let path = `/${owner}/${repo}/compare/${base}`
    let head = document.getElementsByName('head')[0].value;

    if (!isMissing(head)) {
        path = `${path}...${head}`;
    }

    return path;
}
