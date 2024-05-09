# SysProg

Kreirati Web server koji klijentu omogućava pretraživanje pesama uz pomoć Deezer API-a.
Pretraga pesama se može vršiti pomoću Advanced search opcije objašnjene u okviru dokumentacije
(https://developers.deezer.com/api/search).Spisak pesama koje zadovoljavaju kriterijum se vraćaju
kao odgovor klijentu. Svi zahtevi serveru se šalji preko browser-a korišćenjem GET metode.
Ukoliko navedena pesma ne postoji, prikazati grešku klijentu.
Primer poziva servera: https://api.deezer.com/search?q=artist:"coldplay"