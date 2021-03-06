workflow "Push to heroku from master" {
  resolves = ["verify-production"]
  on = "push"
}

action "master-only" {
  uses = "actions/bin/filter@master"
  args = "branch master"
}

action "container-login" {
  uses = "actions/heroku@master"
  needs = ["master-only"]
  args = "container:login"
  secrets = ["HEROKU_API_KEY"]
}

action "push-production" {
  uses = "actions/heroku@master"
  needs = ["container-login"]
  env = {
    HEROKU_APP = "what-did"
  }
  args = ["container:push", "web", "--recursive", "--app", "$HEROKU_APP"]
  secrets = ["HEROKU_API_KEY"]
}

action "release-production" {
  needs = ["push-production"]
  uses = "actions/heroku@master"
  env = {
    HEROKU_APP = "what-did"
  }
  args = ["container:release", "web", "--app", "$HEROKU_APP"]
  secrets = ["HEROKU_API_KEY"]
}

action "verify-production" {
  needs = ["release-production"]
  uses = "actions/heroku@master"
  env = {
    HEROKU_APP = "what-did"
  }
  args = ["apps:info", "$HEROKU_APP"]
  secrets = ["HEROKU_API_KEY"]
}
