class Cloudfox < Formula
  desc "Automating situational awareness for cloud penetration tests"
  homepage "https://bishopfox.com/blog/introducing-cloudfox"
  url "https://github.com/BishopFox/cloudfox/archive/refs/tags/v2.0.5.tar.gz"
  sha256 "642f16328c9c9c6673453d44a361172836c542fb8098285b0cf363e5715a2be0"
  license "MIT"
  head "https://github.com/BishopFox/cloudfox.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "d8e8473abf1e576a40e877287275c429f88f8e1ca81d40a9fb3d568f693d1edd"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
    generate_completions_from_executable(bin/"cloudfox", shell_parameter_format: :cobra)
  end

  test do
    ENV["AWS_ACCESS_KEY_ID"] = "test"
    ENV["AWS_SECRET_ACCESS_KEY"] = "test"

    output = shell_output("#{bin}/cloudfox aws principals 2>&1")
    assert_match "Could not get caller's identity", output

    assert_match version.to_s, shell_output("#{bin}/cloudfox --version")
  end
end
