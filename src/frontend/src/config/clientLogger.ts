type ClientLogLevel = 'warning' | 'error'

const shouldEmitClientEvents = import.meta.env.DEV || import.meta.env.MODE === 'test'
const clientLogEventName = 'storytime:client-log'

const emitClientLogEvent = (level: ClientLogLevel, message: string, error?: unknown): void => {
  if (!shouldEmitClientEvents || typeof window === 'undefined') {
    return
  }

  window.dispatchEvent(
    new CustomEvent(clientLogEventName, {
      detail: {
        level,
        message,
        error,
      },
    }),
  )
}

export const logClientWarning = (message: string, error?: unknown): void => {
  emitClientLogEvent('warning', message, error)
}

export const logClientError = (message: string, error?: unknown): void => {
  emitClientLogEvent('error', message, error)
}
