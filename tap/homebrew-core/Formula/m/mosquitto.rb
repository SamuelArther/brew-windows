class Mosquitto < Formula
  desc "Message broker implementing the MQTT protocol"
  homepage "https://mosquitto.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-mosquitto-2.0.22-2-any.pkg.tar.zst"
  sha256 "8a45a447d31925bdc26d369000b9183472be239f2d70eb3ff457ad5a4e6e93ae"
  license any_of: ["EPL-1.0", "BSD-3-Clause"]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://mosquitto.org/download/"
    regex(/href=.*?mosquitto[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  resource "mingw-w64-ucrt-x86_64-c-ares" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-c-ares-1.34.8-1-any.pkg.tar.zst"
    sha256 "2313d531bc9dc7aae6be3ce0f8ace516bb04191c4f3f62c78c7dfd246d2a9ba6"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-cjson" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cjson-1.7.19-1-any.pkg.tar.zst"
    sha256 "47dc4b9873657a4dfa35f0efe5746b172ee4ae622a12b93be00981a9c17911cd"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libstdc++" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libstdc++-16.2.0-6-any.pkg.tar.zst"
    sha256 "33c5e58a85a538cf843a8672991cce591f0231d66da3b66a57629712d64be17f"
  end

  resource "mingw-w64-ucrt-x86_64-libuv" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libuv-1.53.0-1-any.pkg.tar.zst"
    sha256 "5a89b3afb8ce3a2a6e64704f7fe2ffbf57e1fc88d6033b8dae09a4c74328bcbb"
  end

  resource "mingw-w64-ucrt-x86_64-libwebsockets" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwebsockets-4.5.8-1-any.pkg.tar.zst"
    sha256 "977e54740936f581c4724750b4a6c8ab7f8587a9bf595bc92a87494e5355ee3a"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-openssl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openssl-3.6.5-1-any.pkg.tar.zst"
    sha256 "773e021ebad83f14b5c103db467411e8ff064663e1c7e7f730e2be72f4117064"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/mosquitto.exe", "mosquitto"
    msys2_bin "ucrt64/bin/mosquitto_ctrl.exe", "mosquitto_ctrl"
    msys2_bin "ucrt64/bin/mosquitto_passwd.exe", "mosquitto_passwd"
    msys2_bin "ucrt64/bin/mosquitto_pub.exe", "mosquitto_pub"
    msys2_bin "ucrt64/bin/mosquitto_rr.exe", "mosquitto_rr"
    msys2_bin "ucrt64/bin/mosquitto_sub.exe", "mosquitto_sub"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mosquitto --version 2>&1")
  end
end
