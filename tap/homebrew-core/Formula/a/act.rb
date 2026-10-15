class Act < Formula
  desc "Run your GitHub Actions locally"
  homepage "https://nektosact.com"
  version "0.2.89"
  url "https://github.com/nektos/act/releases/download/v0.2.89/act_Windows_x86_64.zip"
  sha256 "0f8cf866ad7e32421a219fc551a0a0ea5d12ead464de12ebfeafd334a5973b50"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "act.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/act --version")
  end
end
