class Nginx < Formula
  desc "HTTP(S) server and reverse proxy, and IMAP/POP3 proxy server"
  homepage "https://nginx.org/"
  url "https://nginx.org/download/nginx-1.31.7.zip"
  sha256 "7c717ccf861f8925be64431d06d57634512a5014bcc8d149f279928f5e8151bf"
  license "BSD-2-Clause"

  # Official nginx.org Windows build. nginx/Windows resolves conf/, logs/ and temp/ against
  # the current directory, so the launcher passes -p <keg>/libexec/ (a later -p overrides it).

  def install
    install_tree
    windows_exe "libexec/nginx.exe", "nginx", "-p", "#{libexec}/"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/nginx -v 2>&1")

    # nginx reads backslashes in its config as escapes
    dir = testpath.to_s.tr("\\", "/")
    (testpath/"nginx.conf").write <<~NGINX
      worker_processes 4;
      error_log #{dir}/error.log;
      pid #{dir}/nginx.pid;

      events {
        worker_connections 1024;
      }

      http {
        client_body_temp_path #{dir}/client_body_temp;
        fastcgi_temp_path #{dir}/fastcgi_temp;
        proxy_temp_path #{dir}/proxy_temp;
        scgi_temp_path #{dir}/scgi_temp;
        uwsgi_temp_path #{dir}/uwsgi_temp;

        server {
          listen #{free_port};
          root #{dir};
          access_log #{dir}/access.log;
          error_log #{dir}/error.log;
        }
      }
    NGINX
    system bin/"nginx", "-t", "-c", testpath/"nginx.conf"
  end
end
