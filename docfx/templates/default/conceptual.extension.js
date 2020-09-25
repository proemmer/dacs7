// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * This method will be called at the start of exports.transform in conceptual.html.primary.js
 */
exports.preTransform = function (model) {
  model.source = {};
  model.source.remote = {};
  model.source.startLine = 0;
  model.source.remote.path = model._path.substring(0,model._path.length -4) + 'md';
  return model;
}

/**
 * This method will be called at the end of exports.transform in conceptual.html.primary.js
 */
exports.postTransform = function (model) {
  return model;
}