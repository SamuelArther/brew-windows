class Cowsay < Formula
  desc "Apjanke's fork of the classic cowsay project"
  homepage "https://cowsay.diamonds"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cowsay-3.8.4-2-any.pkg.tar.zst"
  sha256 "2eaab1adc80268eeaeb0a3c4b2c3e838efc3e565ff583b7108d78165b7520e1b"
  license "GPL-3.0-only"

  # Native UCRT64 cowsay; the Perl scripts run with a bundled UCRT64 perl.

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

  resource "mingw-w64-ucrt-x86_64-perl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-perl-5.44.0-3-any.pkg.tar.zst"
    sha256 "85d346c0c942d6232df0b32c5d86bb61ed017aba5614d7a9d7ca37f574701bcb"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  def install
    msys2_install
    windows_exe "libexec/ucrt64/bin/perl.exe", "cowsay", "#{libexec}/ucrt64/bin/cowsay"
    windows_exe "libexec/ucrt64/bin/perl.exe", "cowthink", "#{libexec}/ucrt64/bin/cowthink"
  end

  test do
    output = shell_output("#{bin}/cowsay moo")
    assert_match "moo", output
    assert_match "^__^", output
  end
end
