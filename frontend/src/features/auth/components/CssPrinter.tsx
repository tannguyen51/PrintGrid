import './CssPrinter.css'

/** Decorative CSS illustration; no canvas, model download or WebGL required. */
export function CssPrinter() {
  return (
    <div className="auth-printer-scene" aria-hidden="true">
      <div className="auth-printer">
        <div className="auth-printer__shadow" />
        <div className="auth-printer__spool-mount" />
        <div className="auth-printer__spool" />
        <div className="auth-printer__filament" />
        <div className="auth-printer__side" />
        <div className="auth-printer__top" />
        <div className="auth-printer__body">
          <div className="auth-printer__wordmark">PrintGrid <span>3D</span></div>
          <div className="auth-printer__chamber">
            <div className="auth-printer__rail auth-printer__rail--left" />
            <div className="auth-printer__rail auth-printer__rail--right" />
            <div className="auth-printer__bed-support" />
            <div className="auth-printer__bed" />
            <div className="auth-printer__print" />
            <div className="auth-printer__gantry">
              <div className="auth-printer__head">
                <div className="auth-printer__fan" />
                <div className="auth-printer__nozzle" />
              </div>
            </div>
            <div className="auth-printer__glass" />
          </div>
          <div className="auth-printer__console">
            <div className="auth-printer__vents" />
            <div className="auth-printer__screen"><span /><span /><span /></div>
            <div className="auth-printer__dial" />
          </div>
          <div className="auth-printer__accent" />
        </div>
        <div className="auth-printer__foot auth-printer__foot--left" />
        <div className="auth-printer__foot auth-printer__foot--right" />
      </div>
    </div>
  )
}
