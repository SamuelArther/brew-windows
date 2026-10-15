class Shfmt < Formula
  desc "Autoformat shell script source code"
  homepage "https://github.com/mvdan/sh"
  url "https://github.com/mvdan/sh/releases/download/v3.14.1/shfmt_v3.14.1_windows_amd64.exe"
  sha256 "13629ce28442ca80b6b5a819f7574ab39e1c28c6e26734ca816c9714e04851df"
  license "BSD-3-Clause"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "shfmt_v3.14.1_windows_amd64.exe" => "shfmt.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/shfmt --version")
  end
end
