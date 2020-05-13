import { default as Kframe } from '../src/index';

const customKframeUrl = 'https://assist.degdigital.com/@frame';
Kframe.config().kframeUrl = customKframeUrl;

describe('config', () => {
    const config = Kframe.config();
    it('should use custom url', () => {
        expect(config.kframeUrl).toBe(customKframeUrl);
    });
});

describe('frame', () => {
    it('should get frame', () => {
        const frame = Kframe.getFrame();
        expect(frame).toNotBe(null);
    });
});
