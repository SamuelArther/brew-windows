class Hebcal < Formula
  desc "Perpetual Jewish calendar for the command-line"
  homepage "https://hebcal.github.io/"
  url "https://github.com/hebcal/hebcal/archive/refs/tags/v5.17.0.tar.gz"
  sha256 "bee68122bc38f0cdc98bc0001dec4101cf11f78226bf061783c1f5ddf9d52a54"
  license "GPL-2.0-or-later"
  head "https://github.com/hebcal/hebcal.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "37f8b47d2a5d3a9d88bc437e46272a75ad37a294852f1e98adf846ca2840261a"
  end

  depends_on "go" => :build

  deny_network_access!

  def install
    # populate DEFAULT_CITY variable
    system "make", "dcity.go", "man"
    system "go", "build", *std_go_args
    man1.install "hebcal.1"
  end

  test do
    output = shell_output("#{bin}/hebcal 01 01 2020").chomp
    assert_equal output, "1/1/2020 4th of Tevet, 5780"
  end
end
