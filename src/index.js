/* eslint-disable import/no-cycle */
/* eslint-disable no-undef */
import _ from 'lodash';

export const config = () => {
  const configs = document.configs = document.configs || {};
  return configs.kframe = Object.assign(configs.kframe || {}, { kframeUrl: '/@frame' });
}
export const isLoaded = () => !!document.frame;
export const frame = () => document.frame || null;
export const clearFrame = () => { document.frame = null; };

let lookupFrame = () => new Promise((resolve, reject) => {
  let data = {};
  const { kframeUrl } = getConfig();
  fetch(`${kframeUrl}/i`).then((res) => res.json(), reject).then((i) => {
    fetch(`${kframeUrl}/p/${i.frame}`).then((res) => res.json(), reject).then((p) => {
      if (i) {
        _.forOwn(i, (v, k) => {
          if (k !== 'frame') {
            _.pullAllWith(i[k], y.del, (x, y) => y.t === k && x.id === y.id);
            data[k] = _.unionBy(y[k], x[k], 'id');
          }
        });
        return resolve(data);
      }
      console.log('error resolving cache');
      return resolve(null);
    }, reject);
  }, reject)
});

export const getFrame = () => new Promise((resolve, reject) => {
  if (document.frame) {
    return resolve(document.frame);
  }
  // eslint-disable-next-line no-multi-assign
  const queue = (document.frameQueue = document.frameQueue || []);
  queue.push([resolve, reject]);
  if (queue.length === 1) {
    lookupFrame().then((f) => {
      document.frame = f;
      // eslint-disable-next-line no-cond-assign
      let x; while ((x = queue.pop()) !== undefined) x[0](f);
    }, (e) => {
      document.frame = null;
      // eslint-disable-next-line no-cond-assign
      let x; while ((x = queue.pop()) !== undefined) x[1](e);
    });
  }
});

export default {
  config, isLoaded, frame, clearFrame, getFrame
};
