class Pelican < Formula
  desc "Static site generator that supports Markdown and reST syntax"
  homepage "https://getpelican.com/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-pelican-4.12.0-1-any.pkg.tar.zst"
  sha256 "2f900a1619966d426fc565b82bb3ebb549c45b960fb57a8e347cb1b0e94f5867"
  license "AGPL-3.0-or-later"
  revision 2

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://files.pythonhosted.org/packages/c4/8d/da26b77f0d9827d341bdedea357ccb5670717e3a9b4142e3e44c7f34db44/pelican-4.12.0.tar.gz"
  end

  resource "mingw-w64-ucrt-x86_64-bzip2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-bzip2-1.0.8-4-any.pkg.tar.zst"
    sha256 "f03a2174034ddd2d96cecd34f617c5f8e2ef86c812b8d2bb3b8875257f2c8bfa"
  end

  resource "mingw-w64-ucrt-x86_64-cc-libs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cc-libs-16.2.0-6-any.pkg.tar.zst"
    sha256 "d8fff9e62443168bcda8ce746697243519c4f090ea607357ce94feac0bd10a50"
  end

  resource "mingw-w64-ucrt-x86_64-expat" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-expat-2.9.0-1-any.pkg.tar.zst"
    sha256 "f52364dc874009316e1ee1fe46ee89784594b80d10cbcc1cc856945d575308e0"
  end

  resource "mingw-w64-ucrt-x86_64-gettext-runtime" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-gettext-runtime-1.0-1-any.pkg.tar.zst"
    sha256 "ba693dda4ac375af76ce481ff3a6e7481286546cc7dc6d56c7021dae34084157"
  end

  resource "mingw-w64-ucrt-x86_64-libb2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libb2-0.98.1-3-any.pkg.tar.zst"
    sha256 "dfb554635119d3c46adb00c9d0c589bb38d1cb0781b10ecf31de07ce388bd0c0"
  end

  resource "mingw-w64-ucrt-x86_64-libffi" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libffi-3.8.0-1-any.pkg.tar.zst"
    sha256 "99ad12f4ecfa00a889ef9c5c0188592368a5bac109d0c508ce4e04f29ea95a76"
  end

  resource "mingw-w64-ucrt-x86_64-libgcc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgcc-16.2.0-6-any.pkg.tar.zst"
    sha256 "80b2497f019977dd9528b1abc8621b1328a0115ab1f724047ccbb56bf323f11d"
  end

  resource "mingw-w64-ucrt-x86_64-libiconv" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libiconv-1.19-1-any.pkg.tar.zst"
    sha256 "9a500f38c2b91808741c62fae746b3e9110b33a1ecf5c30fa0c66dbedddf7e16"
  end

  resource "mingw-w64-ucrt-x86_64-libstdc++" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libstdc++-16.2.0-6-any.pkg.tar.zst"
    sha256 "33c5e58a85a538cf843a8672991cce591f0231d66da3b66a57629712d64be17f"
  end

  resource "mingw-w64-ucrt-x86_64-libsystre" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libsystre-1.0.2-3-any.pkg.tar.zst"
    sha256 "62f564b1395d4862db1599b77f6588c32de5c80936845751cf369b31eee1560b"
  end

  resource "mingw-w64-ucrt-x86_64-libtre" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libtre-0.9.0-2-any.pkg.tar.zst"
    sha256 "d50f22545c50d7c0cd584474bba83f0c7a2cd8aab1aed1ad6c1f9c6452f3c4fc"
  end

  resource "mingw-w64-ucrt-x86_64-libwinpthread" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libwinpthread-14.0.0.r426.g4564ee4b5-2-any.pkg.tar.zst"
    sha256 "a02f5e09d666d3be657c78c86fe67c975d04fe33b5211cd5ff1b23a91c118524"
  end

  resource "mingw-w64-ucrt-x86_64-mpdecimal" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-mpdecimal-4.0.1-3-any.pkg.tar.zst"
    sha256 "abf25c3869ba5e5bc3b69fdeb6cd49b060fc1a64d0d2de10918adeae9630a68d"
  end

  resource "mingw-w64-ucrt-x86_64-ncurses" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-ncurses-6.6-4-any.pkg.tar.zst"
    sha256 "88331fe78470ef2c5e0280652fee5e10b6dcaa35f8037725c359407f2702309c"
  end

  resource "mingw-w64-ucrt-x86_64-openssl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-openssl-3.6.5-1-any.pkg.tar.zst"
    sha256 "773e021ebad83f14b5c103db467411e8ff064663e1c7e7f730e2be72f4117064"
  end

  resource "mingw-w64-ucrt-x86_64-pcre2" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-pcre2-10.49-1-any.pkg.tar.zst"
    sha256 "63172179db724ac71e2439a7981134c1bcab8049bdddfd349b32b8f76ed52d1e"
  end

  resource "mingw-w64-ucrt-x86_64-python" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-3.14.8-1-any.pkg.tar.zst"
    sha256 "d6542336430039a7cb75f107b713332887665a26a0c9c51e82d8d772c7770b5e"
  end

  resource "mingw-w64-ucrt-x86_64-python-anyio" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-anyio-1~4.15.1-1-any.pkg.tar.zst"
    sha256 "772996a18d60d1b31d709f536ecbfe3bc807cf97916e269a631ba15cc674d7c7"
  end

  resource "mingw-w64-ucrt-x86_64-python-blinker" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-blinker-1.9.0-3-any.pkg.tar.zst"
    sha256 "a98bee86097873a61724fb8bc667b09cf245195de6440e31b2deae7010f3a78f"
  end

  resource "mingw-w64-ucrt-x86_64-python-dateutil" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-dateutil-2.9.0.post0-4-any.pkg.tar.zst"
    sha256 "1a11fefcedd30e622cd0dc6347cbe124a470c2fc61678132300e01c5ee8657cc"
  end

  resource "mingw-w64-ucrt-x86_64-python-docutils" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-docutils-0.22.4-1-any.pkg.tar.zst"
    sha256 "cceda7bb5b939b52ff6cdb1f4a003d47c07d0f4fa60b0f0918349bec457d4dd4"
  end

  resource "mingw-w64-ucrt-x86_64-python-feedgenerator" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-feedgenerator-2.2.1-3-any.pkg.tar.zst"
    sha256 "9db6d255171fe29a5804a048e0de8c1c8fcb4042b3efdb4f245fc3a84da16f97"
  end

  resource "mingw-w64-ucrt-x86_64-python-idna" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-idna-3.20-1-any.pkg.tar.zst"
    sha256 "fa0074ba72a46ec4fe29737ba9965cd11f60c436c16000b0325605db9f682f7f"
  end

  resource "mingw-w64-ucrt-x86_64-python-jinja" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-jinja-3.1.6-4-any.pkg.tar.zst"
    sha256 "5ca05de386e34a689d70785402d3e4563f52104581872b69f61066b689108eba"
  end

  resource "mingw-w64-ucrt-x86_64-python-markdown-it-py" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-markdown-it-py-4.2.0-1-any.pkg.tar.zst"
    sha256 "08c8810f84c7623c7e057ba875c2c7263344116916bc5a60d5d88062c0c19175"
  end

  resource "mingw-w64-ucrt-x86_64-python-markupsafe" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-markupsafe-3.0.4-1-any.pkg.tar.zst"
    sha256 "0004fed7cbba22f61e8df18c854a8dda8c6a257b3b03885fc8c4e940a8df02ab"
  end

  resource "mingw-w64-ucrt-x86_64-python-mdurl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-mdurl-0.1.2-5-any.pkg.tar.zst"
    sha256 "cb416ffd643c06e3cf3f6ff31df6b546a92d15a266772361303c97aff12a75cf"
  end

  resource "mingw-w64-ucrt-x86_64-python-ordered-set" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-ordered-set-4.1.0-7-any.pkg.tar.zst"
    sha256 "fae698bcdedbea4fe8bbf457d73047a08e362a28aceb7f062f0b1ab221a9a014"
  end

  resource "mingw-w64-ucrt-x86_64-python-pygments" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-pygments-2.21.0-1-any.pkg.tar.zst"
    sha256 "2a1ddba5029f1cfb1fc5f1990417960206cd00966b78e4c6db940139e168bf7a"
  end

  resource "mingw-w64-ucrt-x86_64-python-rich" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-rich-15.0.0-1-any.pkg.tar.zst"
    sha256 "0e1a4766706f2c99fe6cbf9b67fe74567b2a7afb64e02891ebc81c2ee6d49742"
  end

  resource "mingw-w64-ucrt-x86_64-python-six" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-six-1.17.0-3-any.pkg.tar.zst"
    sha256 "5fd5f2a4665a50da03bfdc7c59ac00db5a8403a19f1c300ddc2361aba987dbfc"
  end

  resource "mingw-w64-ucrt-x86_64-python-typing_extensions" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-typing_extensions-4.16.0-1-any.pkg.tar.zst"
    sha256 "3a64d230c9e96b6569ce23e804f09b8af8e4bb7c4c805a4c3484a9bb58136fbf"
  end

  resource "mingw-w64-ucrt-x86_64-python-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-tzdata-2026.5-1-any.pkg.tar.zst"
    sha256 "4593ecf1332f987521c887c5d8a77405f7288be399aa4925d38fb6c9c6fa6a8e"
  end

  resource "mingw-w64-ucrt-x86_64-python-unidecode" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-unidecode-1.4.0-3-any.pkg.tar.zst"
    sha256 "be94ffb1736d4362d3c2e20fc3b99f40c86dc034e7a54bd3e145bdf53dba0ec8"
  end

  resource "mingw-w64-ucrt-x86_64-python-watchfiles" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-watchfiles-1.3.0-2-any.pkg.tar.zst"
    sha256 "440a8d7dc3a7ab920132cf8bb62216edd0a49b0a7ff056415725bbcfcf4e4019"
  end

  resource "mingw-w64-ucrt-x86_64-sqlite3" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-sqlite3-3.54.0-1-any.pkg.tar.zst"
    sha256 "fc7100490316733bbb96d5633d305a5d6c579200c321969cfa21d5a128055e65"
  end

  resource "mingw-w64-ucrt-x86_64-tcl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tcl-8.6.18-1-any.pkg.tar.zst"
    sha256 "0c3a50ea5be8c5190e58a1fdc46c09f8113ad1b855745875646b7a78a02215df"
  end

  resource "mingw-w64-ucrt-x86_64-tk" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tk-8.6.18-1-any.pkg.tar.zst"
    sha256 "bfa011886032962833ddc93ae9b508798ec56625b9093cc6d0ff5c95507c79c6"
  end

  resource "mingw-w64-ucrt-x86_64-tzdata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tzdata-2026e-1-any.pkg.tar.zst"
    sha256 "f1809089f8ed5cec858ae8584289172896eb921be1e78ba583555f74e9015428"
  end

  resource "mingw-w64-ucrt-x86_64-wineditline" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-wineditline-2.208-1-any.pkg.tar.zst"
    sha256 "b07b34de1669e16cfd6743691914f5a66ad4013500bbf0e4c7a7644046ae7b1c"
  end

  resource "mingw-w64-ucrt-x86_64-xz" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-xz-5.8.4-1-any.pkg.tar.zst"
    sha256 "a9d771f816d8c0c5bde152ffb27a687ede91079b8e77e5b8297efa3b5a835934"
  end

  resource "mingw-w64-ucrt-x86_64-zlib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-1.3.2-2-any.pkg.tar.zst"
    sha256 "841401182976d2f9e17e5c0ebaac51f2a8014140ea53d67625e91c8fb3c85ea0"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/pelican-import.exe", "pelican-import"
    msys2_bin "ucrt64/bin/pelican-plugins.exe", "pelican-plugins"
    msys2_bin "ucrt64/bin/pelican-quickstart.exe", "pelican-quickstart"
    msys2_bin "ucrt64/bin/pelican-themes.exe", "pelican-themes"
    msys2_bin "ucrt64/bin/pelican.exe", "pelican"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/pelican --version 2>&1")
  end
end
