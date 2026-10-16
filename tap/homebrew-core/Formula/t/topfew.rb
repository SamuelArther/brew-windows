class Topfew < Formula
  desc "Finds the field values which appear most often in a stream of records"
  homepage "https://github.com/timbray/topfew"
  url "https://github.com/timbray/topfew/archive/refs/tags/v2.0.0.tar.gz"
  sha256 "89b9abe7304eb6bb50cc5b3152783e50600439955f73b6175c6db8aec75c0ac9"
  license "GPL-3.0-or-later"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "a46dd429be94bb9ae404c36ec312d8f35fb96c3acb36f1780f2ab289a1c9e9c4"
  end

  depends_on "go" => :build

  deny_network_access!

  def install
    system "go", "build", *std_go_args
  end

  test do
    assert_match "1 bar", pipe_output("#{bin}/topfew -f 2", "foo bar\n")
  end
end
