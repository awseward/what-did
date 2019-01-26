workflow "hello_actions" {
  on = "push"
  resolves = ["login"]
}

action "login" {
  uses = "actions/heroku@6db8f1c22ddf6967566b26d07227c10e8e93844b"
  secrets = ["HEROKU_API_KEY"]
  args = "container:login"
}
