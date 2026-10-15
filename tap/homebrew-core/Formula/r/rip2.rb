class Rip2 < Formula
  desc "Safe and ergonomic alternative to rm"
  homepage "https://github.com/MilesCranmer/rip2"
  url "https://github.com/MilesCranmer/rip2/releases/download/v0.9.7/rip-Windows-x86_64.zip"
  sha256 "f38b7a7c9ecd6a23cf014841fe0da7c1866c9ba6255e6c3ea5e8c8df88063806"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/MilesCranmer/rip2/archive/refs/tags/v0.9.7.tar.gz"
  end

  def install
    bin.install "rip.exe"
  end

  test do
    # Create a test file and verify rip can delete it
    test_file = testpath/"test.txt"
    touch test_file
    system bin/"rip", "--graveyard", testpath/"graveyard", test_file.to_s
    assert_path_exists testpath/"graveyard"
    refute_path_exists test_file
  end
end
