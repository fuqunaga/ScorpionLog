# Changelog

## [0.4.0](https://github.com/fuqunaga/ScotchLog/compare/ga.fuquna.scotch-log-v0.3.1...ga.fuquna.scotch-log-v0.4.0) (2026-05-07)


### Features

* add implicit conversion from ReadOnlySpan&lt;char&gt; to StringWrapper and enable unsafe code ([faf6bbc](https://github.com/fuqunaga/ScotchLog/commit/faf6bbc0635cbacfba724466b3658a7d0671f725))
* implement ConcurrentHashSet for thread-safe collection management ([1f76d1b](https://github.com/fuqunaga/ScotchLog/commit/1f76d1b6d249dab09448b598907893ca8028659b))
* implement ConcurrentObjectPool for efficient object management ([341306c](https://github.com/fuqunaga/ScotchLog/commit/341306cd3fc0cb896e8d04225222b6053997489c))
* implement ConcurrentRingBuffer for thread-safe log entry management ([3e8409a](https://github.com/fuqunaga/ScotchLog/commit/3e8409a64320e770da8b21b7a7f546d63785a4ab))
* introduce LogEntryPersistant for long-term log entry storage and enhance LogScopeRecord with improved property management ([b4cd47e](https://github.com/fuqunaga/ScotchLog/commit/b4cd47ecf51fc376288ff90367d112b6f4ecce00))

## 0.3.1 (2026-03-24)


### Bug Fixes

* LogDispatcher thread safe ([07ace1b](https://github.com/fuqunaga/ScotchLog/commit/07ace1be1a4646fa9f058f9e4bf04e4f6120e6f3))

## [0.3.0](https://github.com/fuqunaga/CategorizedLogging/compare/ga.fuquna.categorized-logging-v0.2.0...ga.fuquna.categorized-logging-v0.3.0) (2026-03-11)


### Features

* implement LogScope and LogProperty ([7a7d3aa](https://github.com/fuqunaga/CategorizedLogging/commit/7a7d3aa7b0ff7cba36679d4131cc848ee475e81c))

## [0.2.0](https://github.com/fuqunaga/CategorizedLogging/compare/ga.fuquna.categorized-logging-v0.1.1...ga.fuquna.categorized-logging-v0.2.0) (2026-03-06)


### Features

* add ListenerSink ([ffb5b99](https://github.com/fuqunaga/CategorizedLogging/commit/ffb5b99e7c0490b188db631e72b54df3de97b66b))
* add Logger. change name Logger to LogDispatcher ([8e45d81](https://github.com/fuqunaga/CategorizedLogging/commit/8e45d8150b0af0d1e8e1b47d9c2321156600eb9a))
* add LogModifier ([b1728bd](https://github.com/fuqunaga/CategorizedLogging/commit/b1728bd2d1eb1991f2e03d7e374e20c5d680d034))


### Bug Fixes

* UnityLoggerSink changes default LogType for LogLevel.Trace to LogType.Log from null ([18a9004](https://github.com/fuqunaga/CategorizedLogging/commit/18a9004e83fb308c4e962ab889b4d1003abd86be))

## [0.1.1](https://github.com/fuqunaga/CategorizedLogging/compare/ga.fuquna.categorized-logging-v0.1.0...ga.fuquna.categorized-logging-v0.1.1) (2026-02-27)


### Bug Fixes

* add HideInCallstackAttribute ([0622c11](https://github.com/fuqunaga/CategorizedLogging/commit/0622c118a0696379a3e4e1916e6f41db56d1bac2))
