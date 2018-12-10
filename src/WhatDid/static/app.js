function isNullOrWhitespace(str) {
  return str == null || str.trim() === '';
}

(() => {

  const app = Stimulus.Application.start();

  app.register('form', class extends Stimulus.Controller {
    static get targets() {
      return ['owner', 'repo', 'base', 'head', 'link'];
    }

    connect() {
      this.update();
    }

    _replacePath(path) { window.history.replaceState({}, null, path); }

    // Yuck, there's probably a less terrible way to implement this.
    _updateBrowserUrl(owner, repo, base, head) {
      if (isNullOrWhitespace(owner)) {
        this._replacePath('/');
      }
      else {
        if (isNullOrWhitespace(repo)) {
          this._replacePath(`/${owner}`);
        } else {
          if (isNullOrWhitespace(base)) {
            this._replacePath(`/${owner}/${repo}`);
          } else {
            let path = `/${owner}/${repo}/compare/${base}`;
            if (!isNullOrWhitespace(head)) {
              path = `${path}...${head}`;
            }
            this._replacePath(path);
          }
        }
      }
    }

    _updateLinkUrl(owner, repo, base, head) {
      const anchor = this.linkTarget;
      if (isNullOrWhitespace(owner) || isNullOrWhitespace(repo) || isNullOrWhitespace(base)) {
        anchor.classList.add('invalid');
        anchor.setAttribute('href', '');
        anchor.innerText = 'Missing some fields which are required to generate a release notes link.';
      } else {
        anchor.classList.remove('invalid');
        let path = `/${owner}/${repo}/compare/${base}`
        if (!isNullOrWhitespace(head)) {
          path = `${path}...${head}`;
        }
        anchor.setAttribute('href', path);
        anchor.innerText = window.location.origin + path;
      }
    }

    update() {
      const owner = this.ownerTarget.value;
      const repo = this.repoTarget.value;
      const base = this.baseTarget.value;
      const head = this.headTarget.value;

      // I'm not sure if I actually like this, so I'm going to leave it
      // commented out for now
      //
      // this._updateBrowserUrl(owner, repo, base, head);
      this._updateLinkUrl(owner, repo, base, head);
    }

    tryNavigateOnEnter(event) {
      if (event.key !== 'Enter') { return; }
      const location = this.linkTarget.getAttribute('href');
      if (!isNullOrWhitespace(location)) {
        Turbolinks.visit(location);
      }
    }
  });

})();
