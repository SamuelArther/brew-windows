class OhMyPosh < Formula
  desc "Prompt theme engine for any shell"
  homepage "https://ohmyposh.dev"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-oh-my-posh-31.7.0-1-any.pkg.tar.zst"
  sha256 "cde014234f6797204de3494ce371d0e2d70f6964c57ccffb17e908f0ee8572b1"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/JanDeDobbeleer/oh-my-posh/archive/refs/tags/v31.8.0.tar.gz"
    strategy :github_latest
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/oh-my-posh.exe", "oh-my-posh"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/oh-my-posh --version 2>&1")
  end
end
