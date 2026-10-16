class Ecoji < Formula
  desc "Encodes (and decodes) data as emojis"
  homepage "https://ecoji.io"
  url "https://github.com/keith-turner/ecoji/archive/refs/tags/v2.0.2.tar.gz"
  sha256 "2f5de343c4e1032b328efe6a3a61d9ba6aae5ef668f99f0d06a16a9dda22e52e"
  license "Apache-2.0"
  head "https://github.com/keith-turner/ecoji.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "b5dfda9c6ae5f755ee8bdf26de92e2223fe35d3a2ed5c4b2dd6b0fa3039c6f90"
  end

  depends_on "go" => :build

  # Add missing go.sum file needed for module verification
  patch do
    url "https://github.com/keith-turner/ecoji/commit/bdf4511e7f4390f0f30c7c51c7607416607a09d0.patch?full_index=1"
    sha256 "37dfa3a641b43acd0bbc805ae2780b5ea9d7e57ecbc73742a7c683a0ffade09e"
    type :backport
    resolves "https://github.com/keith-turner/ecoji/pull/39"
  end

  deny_network_access!

  def fetch
    system "go", "mod", "download", "-C", "cmd/ecoji"
  end

  def install
    cd "cmd/ecoji" do
      system "go", "build", *std_go_args
    end
  end

  test do
    text = "Base64 is so 1999"
    encoded_text = "🧏📩🧈🐇🧅📘🔯🚜💞😽♏🐊🎱🤾☕"
    assert_equal encoded_text, pipe_output("#{bin}/ecoji -e", text).chomp
    assert_equal text, pipe_output("#{bin}/ecoji -d", encoded_text).chomp
  end
end
