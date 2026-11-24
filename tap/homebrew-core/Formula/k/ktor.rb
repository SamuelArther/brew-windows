class Ktor < Formula
  desc "Generates Ktor projects through the command-line interface"
  homepage "https://github.com/ktorio/ktor-cli"
  url "https://github.com/ktorio/ktor-cli/archive/refs/tags/0.5.3.tar.gz"
  sha256 "ce633bc15f8d15f79d438cf6aee1bf005714ae54551cf35dd4f6f2d4381f3fe1"
  license "Apache-2.0"
  head "https://github.com/ktorio/ktor-cli.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "407cd90af3f4730b8b7dd47967aac3796a3e8c790d377bd70307ff2fcd5cafc1"
  end

  depends_on "go" => :build

  def install
    ldflags = "-X main.Version=#{version}"
    system "go", "build", *std_go_args(ldflags:), "./cmd/ktor"
    generate_completions_from_executable(bin/"ktor", "completions")
  end

  test do
    assert_match "Ktor CLI version #{version}", shell_output("#{bin}/ktor --version")
    assert_match "Ktor CLI version #{version}", shell_output("#{bin}/ktor version")
    system bin/"ktor", "new", "project"
    assert_path_exists testpath/"project/build.gradle.kts"
    assert_path_exists testpath/"project/settings.gradle.kts"
    assert_path_exists testpath/"project/gradle.properties"
    assert_path_exists testpath/"project/src"
    assert_path_exists testpath/"project/gradle"
  end
end
