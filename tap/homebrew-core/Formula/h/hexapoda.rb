class Hexapoda < Formula
  desc "Colorful modal hex editor"
  homepage "https://simonomi.dev/hexapoda"
  url "https://github.com/simonomi/hexapoda/releases/download/v1.0.0/hexapoda-Windows-x86_64-v1.0.0.zip"
  sha256 "1e6fc152ae3572f97961b8de845aefb852df2e52c43f446997f5fa61c9258747"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/simonomi/hexapoda/archive/refs/tags/v1.0.0.tar.gz"
  end

  def install
    bin.install "hexapoda.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/hexapoda --version")
    assert_match "hexapoda.toml", shell_output("#{bin}/hexapoda --show-config-path")
  end
end
