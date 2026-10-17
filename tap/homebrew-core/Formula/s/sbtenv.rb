class Sbtenv < Formula
  desc "Command-line tool for managing sbt environments"
  homepage "https://github.com/sbtenv/sbtenv"
  url "https://github.com/sbtenv/sbtenv/archive/refs/tags/version/0.0.24.tar.gz"
  sha256 "f483769e5467c718c9de72baa4eb3c679315e4f4a9ac02bb636996a63c28e3d5"
  license "MIT"
  head "https://github.com/sbtenv/sbtenv.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "595bd42ac1124303e0608021e46ebf5b64cd2579cadfe3de3c4e37bc33817d7e"
  end

  def install
    inreplace "libexec/sbtenv", "/usr/local", HOMEBREW_PREFIX
    prefix.install "bin", "completions", "libexec"
    prefix.install "plugins" => "default-plugins"

    bin.install_symlink prefix/"default-plugins/sbt-install/bin/sbtenv-install"
    prefix.install_symlink (var/"lib/sbtenv/plugins").mkpath
    prefix.install_symlink (var/"lib/sbtenv/versions").mkpath
  end

  # Var symlinks must be done in post install as bottling converts symlinks to real files
  post_install_steps do
    symlink "{{prefix}}/default-plugins/sbt-install", "{{var}}/lib/sbtenv/plugins/sbt-install", overwrite: true
  end

  test do
    shell_output("eval \"$(#{bin}/sbtenv init -)\" && sbtenv versions")
  end
end
