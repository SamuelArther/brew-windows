class Fx < Formula
  desc "Terminal JSON viewer"
  homepage "https://fx.wtf"
  url "https://github.com/antonmedv/fx/releases/download/40.0.0/fx_windows_amd64.exe"
  sha256 "95ad6b786a435c7e06e15c7090f4fe0d6e92ef6f47e9a5e76f493a9626629f70"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "fx_windows_amd64.exe" => "fx.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/fx --version")
  end
end
