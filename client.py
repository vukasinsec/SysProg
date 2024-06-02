import requests
import threading
import time

def send_request(option, query):
    if option == 1:
        url = f"http://localhost:8080/?q=artist:\"{query}\""
    elif option == 2:
        url = f"http://localhost:8080/?q=album:\"{query}\""
    else:
        print("Uneli ste nevažeću opciju.")
        return

    response = requests.get(url)
    print(f"Rezultat za pretragu '{query}': {response.text}")

def create_clients(options, queries, num_clients):
    threads = []
    for _ in range(num_clients):
        for option, query in zip(options, queries):
            thread = threading.Thread(target=send_request, args=(option, query))
            threads.append(thread)
            thread.start()

    for thread in threads:
        thread.join()

if __name__ == "__main__":
    print("Dobrodošli! Molimo odaberite jednu od sledećih opcija:")
    print("1. Pretražite pesme po imenu izvođača")
    print("2. Pretražite pesme po nazivu albuma")

    option = int(input("Unesite broj opcije: "))
    if option not in [1, 2]:
        print("Nevažeći unos. Molimo unesite broj opcije.")
        exit()

    queries = []
    if option == 1:
        artist_name = input("Unesite ime izvođača za pretragu: ")
        if not artist_name:
            print("Niste uneli ništa.")
            exit()
        queries.append(artist_name)
    elif option == 2:
        album_name = input("Unesite ime albuma za pretragu: ")
        if not album_name:
            print("Niste uneli ništa.")
            exit()
        queries.append(album_name)

    num_clients = 50
    print(f"Prvi zahtevi, očekuju se podaci iz API-ja za {num_clients} klijenata:")
    create_clients([option] * len(queries), queries, num_clients)
    time.sleep(2)

    print(f"\nPonovno slanje zahteva, očekujemo podatke iz keša za {num_clients} klijenata:")
    create_clients([option] * len(queries), queries, num_clients)
