class Aiac < Formula
  desc "Artificial Intelligence Infrastructure-as-Code Generator"
  homepage "https://github.com/gofireflyio/aiac"
  url "https://github.com/gofireflyio/aiac/archive/refs/tags/v5.3.0.tar.gz"
  sha256 "45e48cd8958d835b402e0e68d7aa8b9642deb535464dad5d6b83fb8f8ed3d79e"
  license "Apache-2.0"
  head "https://github.com/gofireflyio/aiac.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "976ba2a42cd5f4465e9f23bb6eeb192f587882200c26b564ac85fe2b7f98783f"
  end

  depends_on "go" => :build

  deny_network_access! [:postinstall, :test]

  def install
    ldflags = %W[-X github.com/gofireflyio/aiac/v#{version.major}/libaiac.Version=#{version}]
    system "go", "build", *std_go_args(ldflags:)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/aiac --version")
    assert_match "failed loading configuration", shell_output("#{bin}/aiac python print hello world 2>&1", 1)
  end
end
