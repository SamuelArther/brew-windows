class Nifi < Formula
  desc "Easy to use, powerful, and reliable system to process and distribute data"
  homepage "https://nifi.apache.org"
  url "https://www.apache.org/dyn/closer.lua?path=/nifi/2.12.0/nifi-2.12.0-bin.zip"
  mirror "https://archive.apache.org/dist/nifi/2.12.0/nifi-2.12.0-bin.zip"
  sha256 "46482ddb2a5869a9ef005d7b19a463739695b1a7a8a646030aa3eadc427a61a3"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "53b904c2c81e807eab4e2c9a237adad0f7e17ab7c742c53ced6b150d87540629"
  end

  depends_on "openjdk@21"

  def install
    libexec.install Dir["*"]

    (bin/"nifi").write_env_script libexec/"bin/nifi.sh",
                                  Language::Java.overridable_java_home_env("21").merge(NIFI_HOME: libexec)
  end

  test do
    system bin/"nifi", "status"
  end
end
