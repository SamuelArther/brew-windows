class Onigmo < Formula
  desc "Regular expressions library forked from Oniguruma"
  homepage "https://github.com/k-takata/Onigmo"
  url "https://github.com/k-takata/Onigmo/releases/download/Onigmo-6.2.0/onigmo-6.2.0.tar.gz"
  sha256 "c648496b5339953b925ebf44b8de356feda8d3428fa07dc1db95bfe2570feb76"
  license "BSD-2-Clause"
  compatibility_version 1
  head "https://github.com/k-takata/Onigmo.git", branch: "master"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "30ff420899154024d7fd47e4a9805855d604e32c07154f65424a1d3d005f24c5"
  end

  deny_network_access!

  def install
    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    assert_match(/#{prefix}/, shell_output("#{bin}/onigmo-config --prefix"))
  end
end
