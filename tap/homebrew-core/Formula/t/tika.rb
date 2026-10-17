class Tika < Formula
  desc "Content analysis toolkit"
  homepage "https://tika.apache.org/"
  url "https://www.apache.org/dyn/closer.lua?path=tika/4.1.0/tika-app-4.1.0.zip"
  mirror "https://archive.apache.org/dist/tika/4.1.0/tika-app-4.1.0.zip"
  sha256 "7b570f0a762ee5a94166c9caa1127b00278eaa83a0d5a38633f14c3132734087"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "2ceb5e5f6100aefce7c0a4533d8d1ca54e3d521d2f1f940ca0963568058e1aad"
  end

  depends_on "openjdk"

  resource "server" do
    url "https://www.apache.org/dyn/closer.lua?path=tika/4.1.0/tika-server-standard-4.1.0.zip"
    mirror "https://archive.apache.org/dist/tika/4.1.0/tika-server-standard-4.1.0.zip"
    sha256 "d5d04d44467722e7ce9e7e105638767a6a75715b2d3ac8927c19aaf8758a9284"

    livecheck do
      formula :parent
    end
  end

  allow_network_access! :test

  def install
    odie "update `server` resource" if version != resource("server").version
    libexec.install "tika-app-#{version}.jar"
    bin.write_jar_script libexec/"tika-app-#{version}.jar", "tika"

    libexec.install resource("server")
    bin.write_jar_script libexec/"tika-server-standard-#{version}.jar", "tika-rest-server"
  end

  service do
    run [opt_bin/"tika-rest-server"]
    working_dir var/"tika"
  end

  test do
    pdf = test_fixtures("test.pdf")
    assert_equal "application/pdf\n", shell_output("#{bin}/tika --detect #{pdf}")

    port = free_port
    pid = spawn bin/"tika-rest-server", "--port=#{port}"

    sleep 10
    response = shell_output("curl -s -i http://localhost:#{port}")
    assert_match "HTTP/1.1 200 OK", response
  ensure
    Process.kill("TERM", pid)
    Process.wait(pid)
  end
end
