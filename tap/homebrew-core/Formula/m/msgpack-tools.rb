class MsgpackTools < Formula
  desc "Command-line tools for converting between MessagePack and JSON"
  homepage "https://github.com/ludocode/msgpack-tools"
  url "https://github.com/ludocode/msgpack-tools/releases/download/v0.6/msgpack-tools-0.6.tar.gz"
  sha256 "98c8b789dced626b5b48261b047e2124d256e5b5d4fbbabdafe533c0bd712834"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "43c2e5927df50d7b34f87b5521a0b1caca5b1b314cd5c515e6c8b7cbc775a1a5"
  end

  depends_on "cmake" => :build

  conflicts_with "remarshal", because: "both install 'json2msgpack' binary"

  deny_network_access!

  def install
    # Workaround for CMake 4 compatibility
    args = %w[-DCMAKE_POLICY_VERSION_MINIMUM=3.5]
    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    json_data = <<~JSON
      {"hello":"world"}
    JSON

    msgpack_data = pipe_output("#{bin}/json2msgpack", json_data)
    output = pipe_output("#{bin}/msgpack2json", msgpack_data)
    assert_equal json_data.strip, output.strip
  end
end
