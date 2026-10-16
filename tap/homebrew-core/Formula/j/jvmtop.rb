class Jvmtop < Formula
  desc "Console application for monitoring all running JVMs on a machine"
  homepage "https://github.com/patric-r/jvmtop"
  url "https://github.com/patric-r/jvmtop/releases/download/0.8.0/jvmtop-0.8.0.tar.gz"
  sha256 "f9de8159240b400a51b196520b4c4f0ddbcaa8e587fab1f0a59be8a00dc128c4"
  license "GPL-2.0-only"
  revision 1

  # Deprecated since:
  # * No arm64 macOS support: https://docs.brew.sh/Support-Tiers#future-macos-support
  # * No upstream activity since 2016
  deprecate! date: "2025-09-25", because: :unmaintained
  disable! date: "2026-09-25", because: :unmaintained

  depends_on "openjdk@8"

  on_macos do
    depends_on arch: :x86_64 # openjdk@8 is not supported on ARM
  end

  deny_network_access!

  def install
    rm Dir["*.bat"]
    mv "jvmtop.sh", "jvmtop"
    chmod 0755, "jvmtop"

    libexec.install Dir["*"]
    (bin/"jvmtop").write_env_script(libexec/"jvmtop", Language::Java.java_home_env("1.8"))
  end

  test do
    false
  end
end
