class Kconf < Formula
  desc "CLI for managing multiple kubeconfigs"
  homepage "https://github.com/particledecay/kconf"
  url "https://github.com/particledecay/kconf/releases/download/v2.0.0/kconf-windows-x86_64-2.0.0.tar.gz"
  sha256 "0efc74d555dbe67d76bed1df064c84d8b51923c13779c66fc981d2dc7f6fafad"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/particledecay/kconf/archive/refs/tags/v2.0.0.tar.gz"
  end

  def install
    bin.install "kconf.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/kconf version")

    output = shell_output("#{bin}/kconf namespace homebrew 2>&1", 1)
    expected = "you must first set a current context before setting a preferred namespace"
    assert_match expected, output
  end
end
