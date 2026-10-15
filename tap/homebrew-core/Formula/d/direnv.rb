class Direnv < Formula
  desc "Load/unload environment variables based on $PWD"
  homepage "https://direnv.net/"
  url "https://github.com/direnv/direnv/releases/download/v2.38.2/direnv.windows-amd64.exe"
  sha256 "62a6a16a3473b8e03c2cdc4e2099655bd4284f6da92122b96d78cdce724d4064"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "direnv.windows-amd64.exe" => "direnv.exe"
  end

  test do
    system bin/"direnv", "status"
  end
end
