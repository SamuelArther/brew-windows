class Vegeta < Formula
  desc "HTTP load testing tool and library"
  homepage "https://github.com/tsenart/vegeta"
  url "https://github.com/tsenart/vegeta/releases/download/v12.13.0/vegeta_12.13.0_windows_amd64.zip"
  sha256 "421362edb7c8e1b97db9faf43806d2a8850067afc97c2f1bf9bc5cdcdfc9c0b6"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "vegeta.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/vegeta -version")
  end
end
