class TidyHtml5 < Formula
  desc "Granddaddy of HTML tools, with support for modern standards"
  homepage "https://www.html-tidy.org/"
  url "https://github.com/htacg/tidy-html5/releases/download/5.8.0/tidy-5.8.0-win64.zip"
  sha256 "ffb2f86484c66e70e6a6da369cb8f0f459ad2ffec7c7ca80d09a13445ee51a40"
  license "Zlib"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/bin/tidy.exe", "tidy"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tidy --version")
  end
end
