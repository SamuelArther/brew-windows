class Twine < Formula
  desc "Utilities for interacting with PyPI"
  homepage "https://github.com/pypa/twine"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-twine-7.0.0-1-any.pkg.tar.zst"
  sha256 "1b6a5f7f2ba1c6ff3a3b07276c07ac8574b811e445cc70db9d963cdf2d759183"
  license "Apache-2.0"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://files.pythonhosted.org/packages/92/3c/58f808a359700f39a967dffede33efeac809262c03303fa3eec6afff8f49/twine-7.0.0.tar.gz"
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

  resource "mingw-w64-ucrt-x86_64-python-certifi" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-certifi-2026.7.22-1-any.pkg.tar.zst"
    sha256 "9598f44f2bdce8568eff35b5e7a8168c439d4c0ce597038970a865a16f25cb21"
  end

  resource "mingw-w64-ucrt-x86_64-python-charset-normalizer" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-charset-normalizer-3.5.2-1-any.pkg.tar.zst"
    sha256 "fd2b92b6623b86cbcd235148f30145ee0d9387a86cd49a0ab9f8f8be9e6e42a4"
  end

  resource "mingw-w64-ucrt-x86_64-python-docutils" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-docutils-0.22.4-1-any.pkg.tar.zst"
    sha256 "cceda7bb5b939b52ff6cdb1f4a003d47c07d0f4fa60b0f0918349bec457d4dd4"
  end

  resource "mingw-w64-ucrt-x86_64-python-id" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-id-1.6.1-1-any.pkg.tar.zst"
    sha256 "7ca1486e666b1be2a82d732ef32b5836663873c2ca5d7cb254973b9f83641236"
  end

  resource "mingw-w64-ucrt-x86_64-python-idna" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-idna-3.20-1-any.pkg.tar.zst"
    sha256 "fa0074ba72a46ec4fe29737ba9965cd11f60c436c16000b0325605db9f682f7f"
  end

  resource "mingw-w64-ucrt-x86_64-python-importlib-metadata" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-importlib-metadata-9.0.0-1-any.pkg.tar.zst"
    sha256 "bba40ed475c89b40aff3b6089d7ee2785a1b418f847fadf17e922835635b8580"
  end

  resource "mingw-w64-ucrt-x86_64-python-jaraco.classes" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-jaraco.classes-3.4.0-4-any.pkg.tar.zst"
    sha256 "dfdaf30e29e9ed1960540dfaeff81d7b246dba3124bfb14a370a9cd4a42b84fd"
  end

  resource "mingw-w64-ucrt-x86_64-python-jaraco.context" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-jaraco.context-6.1.2-1-any.pkg.tar.zst"
    sha256 "a1593551ee87f8a483fb8bc7c935e9c61ae81e2cfc50d90f1c26436051c4cc74"
  end

  resource "mingw-w64-ucrt-x86_64-python-jaraco.functools" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-jaraco.functools-4.4.0-3-any.pkg.tar.zst"
    sha256 "e829b7ba5c06103782985884746c8349748132878fc253d61e6a1a483813ffc8"
  end

  resource "mingw-w64-ucrt-x86_64-python-keyring" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-keyring-25.7.0-1-any.pkg.tar.zst"
    sha256 "2738d0a0b35fc3bb0b35aa61303aba9ffe5e5205f837f927775b5b55c7456246"
  end

  resource "mingw-w64-ucrt-x86_64-python-markdown-it-py" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-markdown-it-py-4.2.0-1-any.pkg.tar.zst"
    sha256 "08c8810f84c7623c7e057ba875c2c7263344116916bc5a60d5d88062c0c19175"
  end

  resource "mingw-w64-ucrt-x86_64-python-mdurl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-mdurl-0.1.2-5-any.pkg.tar.zst"
    sha256 "cb416ffd643c06e3cf3f6ff31df6b546a92d15a266772361303c97aff12a75cf"
  end

  resource "mingw-w64-ucrt-x86_64-python-more-itertools" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-more-itertools-11.2.0-1-any.pkg.tar.zst"
    sha256 "917b19b5096c94a93b6f88488b5f0eebc0f841fb9477374316ca068d1326bf75"
  end

  resource "mingw-w64-ucrt-x86_64-python-nh3" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-nh3-0.3.7-1-any.pkg.tar.zst"
    sha256 "9f5dbd5e37d80a07170b6405ad2b314df58738b200ba3b5761e133f61fd340ce"
  end

  resource "mingw-w64-ucrt-x86_64-python-packaging" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-packaging-26.3-1-any.pkg.tar.zst"
    sha256 "b90d1451cfbf015a5334409bd9edf9c61bc0f7b78cac19400b0f92c9279fbaa7"
  end

  resource "mingw-w64-ucrt-x86_64-python-pkginfo" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-pkginfo-1.12.1.2-3-any.pkg.tar.zst"
    sha256 "2e45a3dafe4a5bfbbad9d2ecaaf5f37922e68a909a641d678813bc632af62eec"
  end

  resource "mingw-w64-ucrt-x86_64-python-pygments" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-pygments-2.21.0-1-any.pkg.tar.zst"
    sha256 "2a1ddba5029f1cfb1fc5f1990417960206cd00966b78e4c6db940139e168bf7a"
  end

  resource "mingw-w64-ucrt-x86_64-python-pywin32-ctypes" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-pywin32-ctypes-0.2.3-4-any.pkg.tar.zst"
    sha256 "8b852c7366213c256baadf3083ba51070895d625279a67915e4141f855bf07ba"
  end

  resource "mingw-w64-ucrt-x86_64-python-readme-renderer" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-readme-renderer-44.0-3-any.pkg.tar.zst"
    sha256 "5b1c128d06a5f468c298f142ea5432180d4c2f65312b32ad40ffc5ca008e7de2"
  end

  resource "mingw-w64-ucrt-x86_64-python-requests" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-requests-2.34.2-1-any.pkg.tar.zst"
    sha256 "7a449d97dea8622455bd1065d3e81e5c32ad84b85d8ee0894ad5b5e5d4672cf7"
  end

  resource "mingw-w64-ucrt-x86_64-python-requests-toolbelt" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-requests-toolbelt-1.0.0-5-any.pkg.tar.zst"
    sha256 "7b7b8b0692ee5d18f1305988437e65ff4e50299f6802369f3dff3598e6b86171"
  end

  resource "mingw-w64-ucrt-x86_64-python-rfc3986" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-rfc3986-2.0.0-8-any.pkg.tar.zst"
    sha256 "b480fed29055905a9926bc8db75b786819d81d7ad719816c9b6d6cb6a4fc9f6a"
  end

  resource "mingw-w64-ucrt-x86_64-python-rich" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-rich-15.0.0-1-any.pkg.tar.zst"
    sha256 "0e1a4766706f2c99fe6cbf9b67fe74567b2a7afb64e02891ebc81c2ee6d49742"
  end

  resource "mingw-w64-ucrt-x86_64-python-urllib3" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-python-urllib3-2.8.0-1-any.pkg.tar.zst"
    sha256 "6310d118efb51e9bd14022aa8ddc600dd32ede5929f8d2f20c8c56474458bbf8"
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
    msys2_bin "ucrt64/bin/twine.exe", "twine"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/twine --version 2>&1")
  end
end
