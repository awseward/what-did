module ViewUtils

module Stimulus =
  open Giraffe.GiraffeViewEngine

  let _dataController = attr "data-controller"
  let _dataTarget = attr "data-target"
  let _dataAction = attr "data-action"
