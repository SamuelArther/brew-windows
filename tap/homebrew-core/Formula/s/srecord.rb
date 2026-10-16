class Srecord < Formula
  desc "Tools for manipulating EPROM load files"
  homepage "https://srecord.sourceforge.net/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-srecord-1.65.0-2-any.pkg.tar.zst"
  sha256 "6c48ba6bcf368577739e5fe13e2ea7e34af6ff02aa5f55da309f97d51cf32ba1"
  license all_of: ["GPL-3.0-or-later", "LGPL-3.0-or-later"]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://downloads.sourceforge.net/project/srecord/srecord/1.65/srecord-1.65.0-Source.tar.gz"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-gettext-runtime" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gettext-runtime-1.0-1-any.pkg.tar.zst"
    sha256 "ba693dda4ac375af76ce481ff3a6e7481286546cc7dc6d56c7021dae34084157"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libgcrypt" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcrypt-1.12.4-1-any.pkg.tar.zst"
    sha256 "585121f734c7c00b70274a4cec36c9b7acbfde2402da584a6373f292e2b37fa6"
  end

  resource "mingw-w64-ucrt-x86_64-libgpg-error" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgpg-error-1.61-1-any.pkg.tar.zst"
    sha256 "ca3045875ddae643e6aa927406b8be49670bcdbf31e7c35651bbacf555e95421"
  end

  resource "mingw-w64-ucrt-x86_64-libiconv" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libiconv-1.19-1-any.pkg.tar.zst"
    sha256 "9a500f38c2b91808741c62fae746b3e9110b33a1ecf5c30fa0c66dbedddf7e16"
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
    msys2_bin "ucrt64/bin/srec_cat.exe", "srec_cat"
    msys2_bin "ucrt64/bin/srec_cmp.exe", "srec_cmp"
    msys2_bin "ucrt64/bin/srec_info.exe", "srec_info"
  end

  test do
    (testpath/"test.srec").write <<~EOS
      S012000068656C6C6F5F737265632E73726563F2
      S1130000303132333435363738396162636465668A
      S11300104142434445464748494A4B4C4D4E4F5054
      S10C002041414141424242420ABD
      S9030000FC
    EOS

    expected = <<~EOS
      Format: Motorola S-Record
      Header: "hello_srec.srec"
      Execution Start Address: 00000000
      Data:   0000 - 0028
    EOS

    output = shell_output("#{bin}/srec_info #{testpath}/test.srec")
    assert_equal expected, output

    assert_match version.major_minor.to_s, shell_output("#{bin}/srec_info --version")
  end
end
