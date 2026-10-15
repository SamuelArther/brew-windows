class Chromedriver < Formula
  desc "WebDriver server for Chrome"
  homepage "https://developer.chrome.com/docs/chromedriver"
  url "https://storage.googleapis.com/chrome-for-testing-public/155.0.8059.79/win64/chromedriver-win64.zip"
  version "155.0.8059.79"
  sha256 "95b0a9a7563c339c35eca7146eed19115548f0dc3f816cb1355f20c4eefbeda9"
  license "BSD-3-Clause"

  # Chrome browser is not installed automatically; driver/browser major versions must match.

  def install
    install_tree
    windows_exe "libexec/chromedriver.exe", "chromedriver"
  end
end
