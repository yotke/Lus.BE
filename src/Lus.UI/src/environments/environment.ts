// This file can be replaced during build by using the `fileReplacements` array.
// `ng build --prod` replaces `environment.ts` with `environment.prod.ts`.
// The list of file replacements can be found in `angular.json`.

export const environment = {
  production: false,
  API_KEY: "e8067b53",
  recaptcha: {
    siteKey: '6Lc3p-YnAAAAAAUjmk-ty7qYv70wHnQlxX66Z2uz',
  },
  target: "http://localhost:8088",
  // target: "https://localhost:7236",
  frontTarget: "http://localhost:4200",
  setIdle: 600,  // how long can they be inactive before considered idle, in seconds **[10 hours]**
  setTimeout: 60, // how long can they be idle before considered timed out, in seconds **[10 minutes]**
  keepalive_interval: 30, // will ping at this interval while not idle, in seconds **[10 seconds]**
  keepalive_refresh_interval: 100,// will refresh at this interval (in seconds) the JWT token **[20 seconds]**
  lastPing: 'lastPing',//'cvMpzcnuF0GI5YEnnWLIiwSLJ5xUqGT5+cw24AGwrAUtshprkkGrnAGd8CSRAo8oony2QwU+4wI8VerE4Lw+BkS7dJj5kCasSdArARbgQHwHZd+H0k+AwG6MqDOYsQ==',
  jwt_token: 'jwt_token',//'govgSA40KEP6N8yjgXewFtABDFDuEG9k9RxZyEA+Q2HXbqR1aZE+Vlfybhq3AAON5lhkwEysHN7kLU05ggrdYZAwYXjUK6VXGtmfpNEgQ8ezr5sh2kCPZQijTjLOsg==',
  refresh_token: 'refresh_token',//'RVrC0gFjGkabeahXCLzFcZ7M8XZ0e3MhY2J6DmUAjxGbL9ol+kWSCUY2yVFvAaY4YgJJsY0yMI5d+yPcsAmR5e6bkiEUuQATK+iyjRHA1wH6hAoNekmsAetnwoRNTg=='
  user_data: 'user_data',//'RVrC0gFjGkabeahXCLzFcZ7M8XZ0e3MhY2J6DmUAjxGbL9ol+kWSCUY2yVFvAaY4YgJJsY0yMI5d+yPcsAmR5e6bkiEUuQATK+iyjRHA1wH6hAoNekmsAetnwoRNTg=='
  current_org: 'current_org',
  SignIn_Email: 'israel@test.com',
  SignIn_Cell: '0545745179',
  SignIn_Password: 'AaUu123456@!'
};

/*
 * For easier debugging in development mode, you can import the following file
 * to ignore zone related error stack frames such as `zone.run`, `zoneDelegate.invokeTask`.
 *
 * This import should be commented out in production mode because it will have a negative impact
 * on performance if an error is thrown.
 */
