/* eslint-disable import/no-cycle */
/* eslint-disable no-undef */
import _ from 'lodash';

export const config = () => document.configs.kframe || (() => {
  const configs = document.configs = document.configs || {};
  return configs.kframe = Object.assign(configs.kframe || {}, { kframeUrl: configs.kframeUrl || '/@frame' });
})();

export const isLoaded = () => !!document.frame;
export const frame = () => document.frame || null;
export const clearFrame = () => { document.frame = null; };

let lookupFrame = () => new Promise((resolve, reject) => {
  let data = {};
  const { kframeUrl } = config();
  fetch(`${kframeUrl}/i`).then((res) => res.json(), reject).then((i) => {
    if (!i || !i.length || !i[0].frame) reject();
    else fetch(`${kframeUrl}/p/${i[0].frame}`).then((res) => res.json(), reject).then((p) => {
      i.forEach((ix, idx) => {
        const px = p[idx];
        _.forOwn(ix, (v, k) => {
          if (k !== 'frame') {
            _.pullAllWith(ix[k], px.del, (x, y) => y.t === k && x.id === y.id);
            data[k] = _.unionBy(px[k], ix[k], 'id');
          }
        });
      });
      return resolve(data);
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
      document.frameDate = new Date().getTime();
      // eslint-disable-next-line no-cond-assign
      let x; while ((x = queue.pop()) !== undefined) x[0](f);
    }, (e) => {
      document.frame = null;
      document.frameDate = undefined;
      // eslint-disable-next-line no-cond-assign
      let x; while ((x = queue.pop()) !== undefined) x[1](e);
    });
  }
});

export const checkFrame = (frame, expires) => new Promise((resolve, reject) => {
  if (document.frameDate + expires <= new Date().getTime()) {
    return resolve(frame);
  }
  document.frame = null;
  getFrame().then((f) => resolve(f), (e) => reject(e));
});

export default {
  config, isLoaded, frame, clearFrame, getFrame, checkFrame
};
