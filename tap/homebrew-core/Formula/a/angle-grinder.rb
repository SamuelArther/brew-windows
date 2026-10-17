class AngleGrinder < Formula
  desc "Slice and dice log files on the command-line"
  homepage "https://github.com/rcoh/angle-grinder"
  url "https://github.com/rcoh/angle-grinder/archive/refs/tags/v0.19.6.tar.gz"
  sha256 "f76e236f0825ca3f0b165e37d6448fa36e39c41690e7469d02c37eeb0c972222"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "cddf767b4ad20c1a6c4666f90e16f78dcfa0d183f28eddf8354fb8e0e14a3269"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    (testpath/"logs.txt").write("{\"key\": 5}")
    output = shell_output("#{bin}/agrind --file logs.txt '* | json'")
    assert_match "[key=5]", output
  end
end
