export const environment = {
  production: true,
  recaptcha: {
    siteKey: '6Lc3p-YnAAAAAAUjmk-ty7qYv70wHnQlxX66Z2uz',
  },
  target: "http://localhost:8088",
  frontTarget: "http://localhost:4200",
  setIdle: 600,  // how long can they be inactive before considered idle, in seconds **[10 minutes]**
  setTimeout: 120, // how long can they be idle before considered timed out, in seconds **[2 minutes]**
  keepalive_interval: 30, // will ping at this interval while not idle, in seconds **[30 seconds]**
  keepalive_refresh_interval: 10,// will refresh at this interval (in seconds) the JWT token **[10 seconds]**
  lastPing: 't1q8kqlUjUiONoefCmVpCAbhBRPZU2laCCZTsNwWb3uZQdF3km4HZjXHIqiYwRiqM4R1bvkWqnrQ2rtjaBgUeZzwxLrFUmMOtB5VKHTwcZ2Zw6dr7EKqPu1qTh4VVA==',
  jwt_token: 'fFgVhh0oEuBQ8RbeZ4t4QRVToqWyKky2gk02x2FwvApowMvuVdYkWODP4i9gA5eviN4EVlkmVT2rlpReuIAyvNaGjbpxkGvdC75Hl9UQCgWbbKBtfUGzOyei3LWoJg==',
  refresh_token: 'ODStH9gd0mIFX0nnEyaANgyr96BQkESVlcalSVRDSAjr7bq9cxEqSuV3WaPF9bgQrlO3fYch0eYWM7yJiVwpfg6f1Ass0KlTYJDH8NhAbMTbjDs87ECSUCLniM1buQ==',
  user_data: 'YDfPEPb80K67AfNGxxzZgHTFf0l6M1k28NtaJjj00bgNN5tEGsPNzaNm9jpw1gcoYsqoyUG9nYQqgQoezgZsP2B4ifr0GF7lu3XJZJuAJrtcVrr4902cmuWMb4cd1w==',
  current_org: 'Y3VycmVudF9vcmc=',
  SignIn_Email: '',
  SignIn_Cell: '',
  SignIn_Password: ''
};
