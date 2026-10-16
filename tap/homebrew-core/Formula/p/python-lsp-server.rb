class PythonLspServer < Formula
  desc "Python Language Server for the Language Server Protocol"
  homepage "https://github.com/python-lsp/python-lsp-server"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-lsp-server-1.15.0-1-any.pkg.tar.zst"
  sha256 "a4e6f0c303b726f9e287a869f9a170bb2ee3126025d6e942f63af89fd5bd3197"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://files.pythonhosted.org/packages/e6/63/7d6af072a5b77a0d1f61306b7a72a7a2bc3f29ec0f8a8c85eb23f5ba7716/python_lsp_server-1.15.0.tar.gz"
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

  resource "mingw-w64-ucrt-x86_64-python-black" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-black-26.10.1-1-any.pkg.tar.zst"
    sha256 "46abdd889632a439cdf783a2bd782197624c5e5bbef0f3b81afbe5d821c15323"
  end

  resource "mingw-w64-ucrt-x86_64-python-click" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-click-8.4.2-1-any.pkg.tar.zst"
    sha256 "024147e5ca878603d61f36d66e5d19c8dab2024747381e1464a6a5938e69fc48"
  end

  resource "mingw-w64-ucrt-x86_64-python-colorama" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-colorama-0.4.6-5-any.pkg.tar.zst"
    sha256 "9e2d9e008ee1e1498e00afcbea5a62b46cf98cfae419fc980a5c82032930179f"
  end

  resource "mingw-w64-ucrt-x86_64-python-docstring-to-markdown" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-docstring-to-markdown-0.17-3-any.pkg.tar.zst"
    sha256 "e98f65d0f61ac4c3037eaae0bf2b820ab121ae31cf60db17d64e49e72af3ecc2"
  end

  resource "mingw-w64-ucrt-x86_64-python-importlib-metadata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-importlib-metadata-9.0.0-1-any.pkg.tar.zst"
    sha256 "bba40ed475c89b40aff3b6089d7ee2785a1b418f847fadf17e922835635b8580"
  end

  resource "mingw-w64-ucrt-x86_64-python-jedi" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-jedi-0.20.1-1-any.pkg.tar.zst"
    sha256 "4ec48517a5d206658ad9de99ef48c4f826452a3156b5aabd2910b6cdf8d11036"
  end

  resource "mingw-w64-ucrt-x86_64-python-lsp-jsonrpc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-lsp-jsonrpc-1.1.2-5-any.pkg.tar.zst"
    sha256 "6e39981ed768c5fe7579d3157b87e5b57a0a35748b705fa0e6891ef515a57f22"
  end

  resource "mingw-w64-ucrt-x86_64-python-mypy_extensions" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-mypy_extensions-1.1.0-3-any.pkg.tar.zst"
    sha256 "585a96902ad8f026a5175d9cafca61ce6bb02ad918c81a45be3c303038519d00"
  end

  resource "mingw-w64-ucrt-x86_64-python-packaging" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-packaging-26.3-1-any.pkg.tar.zst"
    sha256 "b90d1451cfbf015a5334409bd9edf9c61bc0f7b78cac19400b0f92c9279fbaa7"
  end

  resource "mingw-w64-ucrt-x86_64-python-parso" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-parso-0.8.7-1-any.pkg.tar.zst"
    sha256 "21fc1d80857e1c96262e78553c4888e363e611204a16df2433638ca60dca56c4"
  end

  resource "mingw-w64-ucrt-x86_64-python-pathspec" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-pathspec-1.1.1-1-any.pkg.tar.zst"
    sha256 "a54a4ba2886d578378a441d712f3b4aad02de70284e8e08d537cd9ea942afe5e"
  end

  resource "mingw-w64-ucrt-x86_64-python-platformdirs" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-platformdirs-4.13.0-1-any.pkg.tar.zst"
    sha256 "6a5fc6a611836b768e3512fe6c9bb0aa392969eeb9bac5b2f17811b9d3efae32"
  end

  resource "mingw-w64-ucrt-x86_64-python-pluggy" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-pluggy-1.7.0-1-any.pkg.tar.zst"
    sha256 "c19c04106e5053ceb711e74fb5ff308911867bc3f212e75e064aad2bf65e8ee3"
  end

  resource "mingw-w64-ucrt-x86_64-python-pytokens" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-pytokens-0.4.1-1-any.pkg.tar.zst"
    sha256 "dc7f538f6bc4e834ab76be3df0df40bb90b01f4f96ebd9e3ff6fa28fb43a094b"
  end

  resource "mingw-w64-ucrt-x86_64-python-typing_extensions" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-typing_extensions-4.16.0-1-any.pkg.tar.zst"
    sha256 "3a64d230c9e96b6569ce23e804f09b8af8e4bb7c4c805a4c3484a9bb58136fbf"
  end

  resource "mingw-w64-ucrt-x86_64-python-ujson" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-ujson-5.13.0-1-any.pkg.tar.zst"
    sha256 "499d8e8674cc6e677f75047d70f2422c38a5bc8ba22f2ffa4468e105bce24cf8"
  end

  resource "mingw-w64-ucrt-x86_64-python-zipp" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-zipp-3.23.1-1-any.pkg.tar.zst"
    sha256 "24fe496f471bdf63323173cc062d3ccd5350e36729f1bcdb401e196312828fd9"
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
    msys2_bin "ucrt64/bin/pylsp.exe", "pylsp"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/pylsp --version 2>&1")
  end
end
