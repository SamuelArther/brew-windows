class Webkitgtk < Formula
  desc "Web engine embedding SDK for Windows apps (Microsoft WebView2)"
  homepage "https://learn.microsoft.com/en-us/microsoft-edge/webview2/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-webview2-loader-1.0.3912.50-1-any.pkg.tar.zst"
  version "1.0.3912.50"
  sha256 "2f4ef97e1ca018425df261ab3e63b738784315d9114cdfaab64340e62fb841fc"
  license "BSD-3-Clause"

  # WebKitGTK embeds WebKit in GTK apps on Linux; WebKit has no maintained Windows port that apps
  # embed. The Windows counterpart is WebView2: the system's Edge (Chromium) web engine, embedded
  # through a COM API. This keg is its SDK as MinGW builds it (MSYS2's webview2-loader package):
  # WebView2.h and WebView2EnvironmentOptions.h, the import library and WebView2Loader.dll.
  # The engine itself, the WebView2 Runtime, ships with Windows 10 and 11.

  livecheck do
    url "https://repo.msys2.org/mingw/ucrt64/"
    regex(/href=.*?mingw-w64-ucrt-x86_64-webview2-loader[._-]v?(\d+(?:\.\d+)+)-\d+-any\.pkg\.t/i)
  end

  def install
    install_tree
    install_library
  end

  def caveats
    <<~EOS
      This is the WebView2 SDK, not the WebKitGTK API: Windows programs embed the web engine
      through ICoreWebView2 (see WebView2.h). The WebView2 Runtime it loads is part of Windows 10/11.
    EOS
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the embedding API and the loader instead.
    header = (include/"WebView2.h").read
    %w[ICoreWebView2 CreateCoreWebView2EnvironmentWithOptions].each { |sym| assert_match sym, header }
    refute_empty Dir["#{prefix}/**/*WebView2Loader*"]
  end
end
