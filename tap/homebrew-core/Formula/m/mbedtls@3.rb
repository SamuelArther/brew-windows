class MbedtlsAT3 < Formula
  desc "Cryptographic & SSL/TLS library"
  homepage "https://www.trustedfirmware.org/projects/mbed-tls/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-mbedtls-3.6.7-1-any.pkg.tar.zst"
  sha256 "9abea4ff9e734cc6444ffa7e4a620141d9db9ba2366904d86ac1e18982f26c26"
  license "Apache-2.0"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/Mbed-TLS/mbedtls/releases/download/mbedtls-3.6.7/mbedtls-3.6.7.tar.bz2"
    regex(/(?:mbedtls[._-])v?(3(?:\.\d+)+)/i)
  end

  keg_only :versioned_formula

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libstdc++" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libstdc++-16.2.0-6-any.pkg.tar.zst"
    sha256 "33c5e58a85a538cf843a8672991cce591f0231d66da3b66a57629712d64be17f"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/mbedtls_aead_demo.exe", "mbedtls_aead_demo"
    msys2_bin "ucrt64/bin/mbedtls_benchmark.exe", "mbedtls_benchmark"
    msys2_bin "ucrt64/bin/mbedtls_cert_app.exe", "mbedtls_cert_app"
    msys2_bin "ucrt64/bin/mbedtls_cert_req.exe", "mbedtls_cert_req"
    msys2_bin "ucrt64/bin/mbedtls_cert_write.exe", "mbedtls_cert_write"
    msys2_bin "ucrt64/bin/mbedtls_cipher_aead_demo.exe", "mbedtls_cipher_aead_demo"
    msys2_bin "ucrt64/bin/mbedtls_crl_app.exe", "mbedtls_crl_app"
    msys2_bin "ucrt64/bin/mbedtls_crypt_and_hash.exe", "mbedtls_crypt_and_hash"
    msys2_bin "ucrt64/bin/mbedtls_crypto_examples.exe", "mbedtls_crypto_examples"
    msys2_bin "ucrt64/bin/mbedtls_dh_client.exe", "mbedtls_dh_client"
    msys2_bin "ucrt64/bin/mbedtls_dh_genprime.exe", "mbedtls_dh_genprime"
    msys2_bin "ucrt64/bin/mbedtls_dh_server.exe", "mbedtls_dh_server"
    msys2_bin "ucrt64/bin/mbedtls_dtls_client.exe", "mbedtls_dtls_client"
    msys2_bin "ucrt64/bin/mbedtls_dtls_server.exe", "mbedtls_dtls_server"
    msys2_bin "ucrt64/bin/mbedtls_ecdh_curve25519.exe", "mbedtls_ecdh_curve25519"
    msys2_bin "ucrt64/bin/mbedtls_ecdsa.exe", "mbedtls_ecdsa"
    msys2_bin "ucrt64/bin/mbedtls_gen_entropy.exe", "mbedtls_gen_entropy"
    msys2_bin "ucrt64/bin/mbedtls_gen_key.exe", "mbedtls_gen_key"
    msys2_bin "ucrt64/bin/mbedtls_gen_random_ctr_drbg.exe", "mbedtls_gen_random_ctr_drbg"
    msys2_bin "ucrt64/bin/mbedtls_generic_sum.exe", "mbedtls_generic_sum"
    msys2_bin "ucrt64/bin/mbedtls_hello.exe", "mbedtls_hello"
    msys2_bin "ucrt64/bin/mbedtls_hmac_demo.exe", "mbedtls_hmac_demo"
    msys2_bin "ucrt64/bin/mbedtls_key_app.exe", "mbedtls_key_app"
    msys2_bin "ucrt64/bin/mbedtls_key_app_writer.exe", "mbedtls_key_app_writer"
    msys2_bin "ucrt64/bin/mbedtls_key_ladder_demo.exe", "mbedtls_key_ladder_demo"
    msys2_bin "ucrt64/bin/mbedtls_load_roots.exe", "mbedtls_load_roots"
    msys2_bin "ucrt64/bin/mbedtls_md_hmac_demo.exe", "mbedtls_md_hmac_demo"
    msys2_bin "ucrt64/bin/mbedtls_metatest.exe", "mbedtls_metatest"
    msys2_bin "ucrt64/bin/mbedtls_mini_client.exe", "mbedtls_mini_client"
    msys2_bin "ucrt64/bin/mbedtls_mpi_demo.exe", "mbedtls_mpi_demo"
    msys2_bin "ucrt64/bin/mbedtls_pem2der.exe", "mbedtls_pem2der"
    msys2_bin "ucrt64/bin/mbedtls_pk_decrypt.exe", "mbedtls_pk_decrypt"
    msys2_bin "ucrt64/bin/mbedtls_pk_encrypt.exe", "mbedtls_pk_encrypt"
    msys2_bin "ucrt64/bin/mbedtls_pk_sign.exe", "mbedtls_pk_sign"
    msys2_bin "ucrt64/bin/mbedtls_pk_verify.exe", "mbedtls_pk_verify"
    msys2_bin "ucrt64/bin/mbedtls_psa_constant_names.exe", "mbedtls_psa_constant_names"
    msys2_bin "ucrt64/bin/mbedtls_psa_hash.exe", "mbedtls_psa_hash"
    msys2_bin "ucrt64/bin/mbedtls_query_compile_time_config.exe", "mbedtls_query_compile_time_config"
    msys2_bin "ucrt64/bin/mbedtls_query_included_headers.exe", "mbedtls_query_included_headers"
    msys2_bin "ucrt64/bin/mbedtls_req_app.exe", "mbedtls_req_app"
    msys2_bin "ucrt64/bin/mbedtls_rsa_decrypt.exe", "mbedtls_rsa_decrypt"
    msys2_bin "ucrt64/bin/mbedtls_rsa_encrypt.exe", "mbedtls_rsa_encrypt"
    msys2_bin "ucrt64/bin/mbedtls_rsa_genkey.exe", "mbedtls_rsa_genkey"
    msys2_bin "ucrt64/bin/mbedtls_rsa_sign.exe", "mbedtls_rsa_sign"
    msys2_bin "ucrt64/bin/mbedtls_rsa_sign_pss.exe", "mbedtls_rsa_sign_pss"
    msys2_bin "ucrt64/bin/mbedtls_rsa_verify.exe", "mbedtls_rsa_verify"
    msys2_bin "ucrt64/bin/mbedtls_rsa_verify_pss.exe", "mbedtls_rsa_verify_pss"
    msys2_bin "ucrt64/bin/mbedtls_selftest.exe", "mbedtls_selftest"
    msys2_bin "ucrt64/bin/mbedtls_ssl_client1.exe", "mbedtls_ssl_client1"
    msys2_bin "ucrt64/bin/mbedtls_ssl_client2.exe", "mbedtls_ssl_client2"
    msys2_bin "ucrt64/bin/mbedtls_ssl_context_info.exe", "mbedtls_ssl_context_info"
    msys2_bin "ucrt64/bin/mbedtls_ssl_fork_server.exe", "mbedtls_ssl_fork_server"
    msys2_bin "ucrt64/bin/mbedtls_ssl_mail_client.exe", "mbedtls_ssl_mail_client"
    msys2_bin "ucrt64/bin/mbedtls_ssl_pthread_server.exe", "mbedtls_ssl_pthread_server"
    msys2_bin "ucrt64/bin/mbedtls_ssl_server.exe", "mbedtls_ssl_server"
    msys2_bin "ucrt64/bin/mbedtls_ssl_server2.exe", "mbedtls_ssl_server2"
    msys2_bin "ucrt64/bin/mbedtls_strerror.exe", "mbedtls_strerror"
    msys2_bin "ucrt64/bin/mbedtls_udp_proxy.exe", "mbedtls_udp_proxy"
    msys2_bin "ucrt64/bin/mbedtls_zeroize.exe", "mbedtls_zeroize"
  end

  test do
    assert_path_exists bin/"mbedtls_aead_demo.exe"
  end
end
