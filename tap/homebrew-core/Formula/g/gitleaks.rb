class Gitleaks < Formula
  desc "Audit git repos for secrets"
  homepage "https://gitleaks.io/"
  url "https://github.com/gitleaks/gitleaks/releases/download/v8.30.1/gitleaks_8.30.1_windows_x64.zip"
  sha256 "d29144deff3a68aa93ced33dddf84b7fdc26070add4aa0f4513094c8332afc4e"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "gitleaks.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gitleaks version")
  end
end
