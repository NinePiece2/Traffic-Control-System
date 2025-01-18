import requests

token_url = "https://api-trafficcontrolsystem.romitsagu.com/Token/GetToken?userID=xrUXvjdVt0Kz3c7drkt9oSLR9Rcm5nPQXe8rU1riDT2qKLzPlH9VsetNzv68YHqG%2Bn%2BTWeP4ZSqZnBpNEc6UKpZVrleN6Yr5rYwhg7qhXQvQ5eohikcuyxFVbDATEK4aqEoRheBEoDh4VpDzOTi%2BS9zVE7p9f%2F%2B6l6ZPPtIhu2U%3D"

headers = {
    'Authorization': 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6Ik9SbUVxR1VZeUphQzJHVmdHT0tiNXlJUVI5dUdlMUNhN0FLYldoZzc3Mi91L1Jxam1lK0NPTkJkNnpVWFNsbU1wQzkrU3JMSUZYeXJmOVhoTUVLakZhcEROZ2N3ampIcjNBQmFvdTQ1MXRyYVdGd2V2d1BzVTlKS3dyaFV5TWQ5T1FySytYUFlScFVXK2pOZE9yQUxYaWRGN2prR0lTZnhMUHVuUWxkLzVOaz0iLCJuYmYiOjE3MzY1NzgwMzcsImV4cCI6MTczNjU4MTYzNywiaWF0IjoxNzM2NTc4MDM3LCJpc3MiOiJodHRwczovL2xvY2FsaG9zdDo0NDM2MyIsImF1ZCI6Imh0dHBzOi8vbG9jYWxob3N0OjQ0MzYzIn0.akiZNuRx5tPXJTeQr1GudV3PTQgQcsQQP3a54qvINDk'
}

response_token = requests.get(token_url, headers=headers)

if response_token.status_code == 200:
    token = response_token.text
    print(f"Token received: {token}")

    stream_url = "https://api-trafficcontrolsystem.romitsagu.com/Traffic/GetStreamClientKey?DeviceStreamID=device1"

    headers['Authorization'] = f'Bearer {token}'

    response_stream = requests.get(stream_url, headers=headers)

    if response_stream.status_code == 200:
        print(f"Stream Client Key Response: {response_stream.text}")
    else:
        print(f"Failed to fetch stream client key. Status code: {response_stream.status_code}")
        print(f"Response Content: {response_stream.text}")
else:
    print(f"Failed to get token. Status code: {response_token.status_code}")
    print(f"Response Content: {response_token.text}")
