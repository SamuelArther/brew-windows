class Chezmoi < Formula
  desc "Manage your dotfiles across multiple diverse machines, securely"
  homepage "https://chezmoi.io/"
  url "https://github.com/twpayne/chezmoi/releases/download/v2.73.1/chezmoi_2.73.1_windows_amd64.zip"
  sha256 "29c48a8d403f590cfacf16fbf9db3c2dc0a8ebe8b744db64bac6057c0266c65d"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "chezmoi.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/chezmoi --version")
  end
end
