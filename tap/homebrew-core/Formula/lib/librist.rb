class Librist < Formula
  desc "Reliable Internet Stream Transport (RIST)"
  homepage "https://code.videolan.org/rist/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-librist-0.2.20-2-any.pkg.tar.zst"
  sha256 "f573ecfc4c427e2b3ea697d12b7eeeea5f4bee3664392fe73fcd3d677f6b7d88"
  license "BSD-2-Clause"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://code.videolan.org/rist/librist/-/archive/v0.2.20/librist-v0.2.20.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
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

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-lz4" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-lz4-1.10.0-1-any.pkg.tar.zst"
    sha256 "00a807df81a4b7e82445178b8625962ed592cf1f0a08a8e04c8b9338c4362638"
  end

  resource "mingw-w64-ucrt-x86_64-mbedtls" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-mbedtls-3.6.7-1-any.pkg.tar.zst"
    sha256 "9abea4ff9e734cc6444ffa7e4a620141d9db9ba2366904d86ac1e18982f26c26"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/rist2rist.exe", "rist2rist"
    msys2_bin "ucrt64/bin/ristreceiver.exe", "ristreceiver"
    msys2_bin "ucrt64/bin/ristsender.exe", "ristsender"
    msys2_bin "ucrt64/bin/ristsrppasswd.exe", "ristsrppasswd"
    msys2_bin "ucrt64/bin/risttunnel.exe", "risttunnel"
    msys2_bin "ucrt64/bin/udp2udp.exe", "udp2udp"
  end

  test do
    assert_match "Starting ristsender", shell_output("#{bin}/ristsender 2>&1", 1)
  end
end
