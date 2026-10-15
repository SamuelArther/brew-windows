class Sops < Formula
  desc "Editor of encrypted files"
  homepage "https://getsops.io/"
  url "https://github.com/getsops/sops/releases/download/v3.13.3/sops-v3.13.3.amd64.exe"
  sha256 "a4a9a398858fe8b2ef72d9686d930bf7c5cece9be74ad83ac3b53cfdd70e6b1c"
  license "MPL-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "sops-v3.13.3.amd64.exe" => "sops.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/sops --version")
  end
end
