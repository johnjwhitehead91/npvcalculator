export const environment = {
  production: false,
  apiUrl: 'https://localhost:44355/api', // .NET API URL (development)
  apiEndpoints: {
    npv: {
      calculate: '/npv/calculate',
      status: '/npv/status',
      results: '/npv/results'
    }
  }
};
