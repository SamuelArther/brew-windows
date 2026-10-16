class Libwpe < Formula
  desc "General-purpose web view embedding library (webview on WebView2)"
  homepage "https://github.com/webview/webview"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-webview-0.12.0-3-any.pkg.tar.zst"
  version "0.12.0"
  sha256 "429d470e94fb4b9aca076acd85a870dda460c1f774e8677e8304038d7a9719df"
  license "MIT"

  # libwpe is the backend-neutral layer WPE WebKit is embedded through. WPE WebKit doesn't run on
  # Windows; the backend-neutral embedding library there is webview (the same C API on WebKitGTK,
  # Cocoa WebKit and WebView2), which on Windows drives WebView2 from the webkitgtk formula.
  # Prebuilt UCRT64 package from MSYS2 with its C++ runtime pinned as resources.

  livecheck do
    url "https://repo.msys2.org/mingw/ucrt64/"
    regex(/href=.*?mingw-w64-ucrt-x86_64-webview[._-]v?(\d+(?:\.\d+)+)-\d+-any\.pkg\.t/i)
  end

  depends_on "webkitgtk"

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

  def install
    install_tree
    install_resource "mingw-w64-ucrt-x86_64-cc-libs", "libexec"
    install_resource "mingw-w64-ucrt-x86_64-libgcc", "libexec"
    install_resource "mingw-w64-ucrt-x86_64-libstdc++", "libexec"
    install_resource "mingw-w64-ucrt-x86_64-libwinpthread", "libexec"
    install_library
  end

  def caveats
    <<~EOS
      This is the webview C API (webview_create, webview_navigate, webview_bind...), not libwpe's
      wpe_* backend API: WPE WebKit has no Windows port.
    EOS
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the embedding API and libraries instead.
    headers = Dir["#{include}/**/webview*.h"]
    refute_empty headers
    assert_match "webview_create", headers.map { |h| File.read(h) }.join
    refute_empty Dir["#{lib}/*webview*"]
  end
end
