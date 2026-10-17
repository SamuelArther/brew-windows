class Riemann < Formula
  desc "Event stream processor"
  homepage "https://riemann.io/"
  url "https://github.com/riemann/riemann/releases/download/0.3.12/riemann-0.3.12.tar.bz2"
  sha256 "82c24c7cba3bce96957f25661f39c6162a262ba76aef24e986e73dbf2a79b7a6"
  license "EPL-1.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "4f560e0439755bffecdabbca077f2a670089e475517c5fd57b8ed565bea6d6c4"
  end

  depends_on "openjdk"

  def install
    inreplace "bin/riemann", "$top/etc", etc
    etc.install "etc/riemann.config" => "riemann.config.guide"

    # Install jars in libexec to avoid conflicts
    libexec.install Dir["*"]

    (bin/"riemann").write_env_script libexec/"bin/riemann", Language::Java.overridable_java_home_env
  end

  def caveats
    <<~EOS
      You may also wish to install these Ruby gems:
        riemann-client
        riemann-tools
        riemann-dash
    EOS
  end

  service do
    run [opt_bin/"riemann", etc/"riemann.config"]
    keep_alive true
    log_path var/"log/riemann.log"
    error_log_path var/"log/riemann.log"
  end

  test do
    system bin/"riemann", "-help", "0"
  end
end
