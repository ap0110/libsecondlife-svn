//
// basic_socket_acceptor.hpp
// ~~~~~~~~~~~~~~~~~~~~~~~~~
//
// Copyright (c) 2003-2005 Christopher M. Kohlhoff (chris at kohlhoff dot com)
//
// Distributed under the Boost Software License, Version 1.0. (See accompanying
// file LICENSE_1_0.txt or copy at http://www.boost.org/LICENSE_1_0.txt)
//

#ifndef BOOST_ASIO_BASIC_SOCKET_ACCEPTOR_HPP
#define BOOST_ASIO_BASIC_SOCKET_ACCEPTOR_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <boost/asio/detail/push_options.hpp>

#include <boost/asio/error.hpp>
#include <boost/asio/error_handler.hpp>
#include <boost/asio/service_factory.hpp>
#include <boost/asio/socket_base.hpp>
#include <boost/asio/detail/noncopyable.hpp>

namespace boost {
namespace asio {

/// Provides the ability to accept new connections.
/**
 * The basic_socket_acceptor class template is used for accepting new socket
 * connections.
 *
 * Most applications would use the boost::asio::socket_acceptor typedef.
 *
 * @par Thread Safety:
 * @e Distinct @e objects: Safe.@n
 * @e Shared @e objects: Unsafe.
 *
 * @par Concepts:
 * Async_Object, Error_Source.
 *
 * @par Example:
 * Opening a socket acceptor with the SO_REUSEADDR option enabled:
 * @code
 * boost::asio::socket_acceptor acceptor(demuxer);
 * boost::asio::ipv4::tcp::endpoint endpoint(port);
 * acceptor.open(endpoint.protocol());
 * acceptor.set_option(boost::asio::socket_acceptor::reuse_address(true));
 * acceptor.bind(endpoint);
 * acceptor.listen();
 * @endcode
 */
template <typename Service>
class basic_socket_acceptor
  : public socket_base,
    private noncopyable
{
public:
  /// The type of the service that will be used to provide accept operations.
  typedef Service service_type;

  /// The native implementation type of the socket acceptor.
  typedef typename service_type::impl_type impl_type;

  /// The demuxer type for this asynchronous type.
  typedef typename service_type::demuxer_type demuxer_type;

  /// The type used for reporting errors.
  typedef boost::asio::error error_type;

  /// Construct an acceptor without opening it.
  /**
   * This constructor creates an acceptor without opening it to listen for new
   * connections. The open() function must be called before the acceptor can
   * accept new socket connections.
   *
   * @param d The demuxer object that the acceptor will use to dispatch
   * handlers for any asynchronous operations performed on the acceptor.
   */
  explicit basic_socket_acceptor(demuxer_type& d)
    : service_(d.get_service(service_factory<Service>())),
      impl_(service_.null())
  {
  }

  /// Construct an acceptor opened on the given endpoint.
  /**
   * This constructor creates an acceptor and automatically opens it to listen
   * for new connections on the specified endpoint.
   *
   * @param d The demuxer object that the acceptor will use to dispatch
   * handlers for any asynchronous operations performed on the acceptor.
   *
   * @param endpoint An endpoint on the local machine on which the acceptor
   * will listen for new connections.
   *
   * @param listen_backlog The maximum length of the queue of pending
   * connections. A value of 0 means use the default queue length.
   *
   * @throws boost::asio::error Thrown on failure.
   *
   * @note This constructor is equivalent to the following code:
   * @code
   * boost::asio::socket_acceptor acceptor(demuxer);
   * acceptor.open(endpoint.protocol());
   * acceptor.bind(endpoint);
   * acceptor.listen(listen_backlog);
   * @endcode
   */
  template <typename Endpoint>
  basic_socket_acceptor(demuxer_type& d, const Endpoint& endpoint,
      int listen_backlog = 0)
    : service_(d.get_service(service_factory<Service>())),
      impl_(service_.null())
  {
    service_.open(impl_, endpoint.protocol(), throw_error());
    close_on_block_exit auto_close(service_, impl_);
    service_.bind(impl_, endpoint, throw_error());
    service_.listen(impl_, listen_backlog, throw_error());
    auto_close.cancel();
  }

  /// Destructor.
  ~basic_socket_acceptor()
  {
    service_.close(impl_, ignore_error());
  }

  /// Get the demuxer associated with the asynchronous object.
  /**
   * This function may be used to obtain the demuxer object that the acceptor
   * uses to dispatch handlers for asynchronous operations.
   *
   * @return A reference to the demuxer object that acceptor will use to
   * dispatch handlers. Ownership is not transferred to the caller.
   */
  demuxer_type& demuxer()
  {
    return service_.demuxer();
  }

  /// Open the acceptor using the specified protocol.
  /**
   * This function opens the socket acceptor so that it will use the specified
   * protocol.
   *
   * @param protocol An object specifying which protocol is to be used.
   *
   * @par Example:
   * @code
   * boost::asio::socket_acceptor acceptor(demuxer);
   * acceptor.open(boost::asio::ipv4::tcp());
   * @endcode
   */
  template <typename Protocol>
  void open(const Protocol& protocol)
  {
    service_.open(impl_, protocol, throw_error());
  }

  /// Open the acceptor using the specified protocol.
  /**
   * This function opens the socket acceptor so that it will use the specified
   * protocol.
   *
   * @param protocol An object specifying which protocol is to be used.
   *
   * @param error_handler The handler to be called when an error occurs. Copies
   * will be made of the handler as required. The function signature of the
   * handler must be:
   * @code void error_handler(
   *   const boost::asio::error& error // Result of operation
   * ); @endcode
   *
   * @par Example:
   * @code
   * boost::asio::socket_acceptor acceptor(demuxer);
   * boost::asio::error error;
   * acceptor.open(boost::asio::ipv4::tcp(), boost::asio::assign_error(error));
   * if (error)
   * {
   *   // An error occurred.
   * }
   * @endcode
   */
  template <typename Protocol, typename Error_Handler>
  void open(const Protocol& protocol, Error_Handler error_handler)
  {
    service_.open(impl_, protocol, error_handler);
  }

  /// Bind the acceptor to the given local endpoint.
  /**
   * This function binds the socket acceptor to the specified endpoint on the
   * local machine.
   *
   * @param endpoint An endpoint on the local machine to which the socket
   * acceptor will be bound.
   *
   * @throws boost::asio::error Thrown on failure.
   *
   * @par Example:
   * @code
   * boost::asio::socket_acceptor acceptor(demuxer);
   * acceptor.open(boost::asio::ipv4::tcp());
   * acceptor.bind(boost::asio::ipv4::tcp::endpoint(12345));
   * @endcode
   */
  template <typename Endpoint>
  void bind(const Endpoint& endpoint)
  {
    service_.bind(impl_, endpoint, throw_error());
  }

  /// Bind the acceptor to the given local endpoint.
  /**
   * This function binds the socket acceptor to the specified endpoint on the
   * local machine.
   *
   * @param endpoint An endpoint on the local machine to which the socket
   * acceptor will be bound.
   *
   * @param error_handler The handler to be called when an error occurs. Copies
   * will be made of the handler as required. The function signature of the
   * handler must be:
   * @code void error_handler(
   *   const boost::asio::error& error // Result of operation
   * ); @endcode
   *
   * @par Example:
   * @code
   * boost::asio::socket_acceptor acceptor(demuxer);
   * acceptor.open(boost::asio::ipv4::tcp());
   * boost::asio::error error;
   * acceptor.bind(boost::asio::ipv4::tcp::endpoint(12345),
   *     boost::asio::assign_error(error));
   * if (error)
   * {
   *   // An error occurred.
   * }
   * @endcode
   */
  template <typename Endpoint, typename Error_Handler>
  void bind(const Endpoint& endpoint, Error_Handler error_handler)
  {
    service_.bind(impl_, endpoint, error_handler);
  }

  /// Place the acceptor into the state where it will listen for new
  /// connections.
  /**
   * This function puts the socket acceptor into the state where it may accept
   * new connections.
   *
   * @param backlog The maximum length of the queue of pending connections. A
   * value of 0 means use the default queue length.
   */
  void listen(int backlog = 0)
  {
    service_.listen(impl_, backlog, throw_error());
  }

  /// Place the acceptor into the state where it will listen for new
  /// connections.
  /**
   * This function puts the socket acceptor into the state where it may accept
   * new connections.
   *
   * @param backlog The maximum length of the queue of pending connections. A
   * value of 0 means use the default queue length.
   *
   * @param error_handler The handler to be called when an error occurs. Copies
   * will be made of the handler as required. The function signature of the
   * handler must be:
   * @code void error_handler(
   *   const boost::asio::error& error // Result of operation
   * ); @endcode
   *
   * @par Example:
   * @code
   * boost::asio::socket_acceptor acceptor(demuxer);
   * ...
   * boost::asio::error error;
   * acceptor.listen(0, boost::asio::assign_error(error));
   * if (error)
   * {
   *   // An error occurred.
   * }
   * @endcode
   */
  template <typename Error_Handler>
  void listen(int backlog, Error_Handler error_handler)
  {
    service_.listen(impl_, backlog, error_handler);
  }

  /// Close the acceptor.
  /**
   * This function is used to close the acceptor. Any asynchronous accept
   * operations will be cancelled immediately.
   *
   * A subsequent call to open() is required before the acceptor can again be
   * used to again perform socket accept operations.
   *
   * @throws boost::asio::error Thrown on failure.
   */
  void close()
  {
    service_.close(impl_, throw_error());
  }

  /// Close the acceptor.
  /**
   * This function is used to close the acceptor. Any asynchronous accept
   * operations will be cancelled immediately.
   *
   * A subsequent call to open() is required before the acceptor can again be
   * used to again perform socket accept operations.
   *
   * @param error_handler The handler to be called when an error occurs. Copies
   * will be made of the handler as required. The function signature of the
   * handler must be:
   * @code void error_handler(
   *   const boost::asio::error& error // Result of operation
   * ); @endcode
   *
   * @par Example:
   * @code
   * boost::asio::socket_acceptor acceptor(demuxer);
   * ...
   * boost::asio::error error;
   * acceptor.close(boost::asio::assign_error(error));
   * if (error)
   * {
   *   // An error occurred.
   * }
   * @endcode
   */
  template <typename Error_Handler>
  void close(Error_Handler error_handler)
  {
    service_.close(impl_, error_handler);
  }

  /// Get the underlying implementation in the native type.
  /**
   * This function may be used to obtain the underlying implementation of the
   * socket acceptor. This is intended to allow access to native socket
   * functionality that is not otherwise provided.
   */
  impl_type impl()
  {
    return impl_;
  }

  /// Set an option on the acceptor.
  /**
   * This function is used to set an option on the acceptor.
   *
   * @param option The new option value to be set on the acceptor.
   *
   * @throws boost::asio::error Thrown on failure.
   *
   * @sa Socket_Option @n
   * boost::asio::socket_base::reuse_address
   *
   * @par Example:
   * Setting the SOL_SOCKET/SO_REUSEADDR option:
   * @code
   * boost::asio::socket_acceptor acceptor(demuxer);
   * ...
   * boost::asio::socket_acceptor::reuse_address option(true);
   * acceptor.set_option(option);
   * @endcode
   */
  template <typename Option>
  void set_option(const Option& option)
  {
    service_.set_option(impl_, option, throw_error());
  }

  /// Set an option on the acceptor.
  /**
   * This function is used to set an option on the acceptor.
   *
   * @param option The new option value to be set on the acceptor.
   *
   * @param error_handler The handler to be called when an error occurs. Copies
   * will be made of the handler as required. The function signature of the
   * handler must be:
   * @code void error_handler(
   *   const boost::asio::error& error // Result of operation
   * ); @endcode
   *
   * @sa Socket_Option @n
   * boost::asio::socket_base::reuse_address
   *
   * @par Example:
   * Setting the SOL_SOCKET/SO_REUSEADDR option:
   * @code
   * boost::asio::socket_acceptor acceptor(demuxer);
   * ...
   * boost::asio::socket_acceptor::reuse_address option(true);
   * boost::asio::error error;
   * acceptor.set_option(option, boost::asio::assign_error(error));
   * if (error)
   * {
   *   // An error occurred.
   * }
   * @endcode
   */
  template <typename Option, typename Error_Handler>
  void set_option(const Option& option, Error_Handler error_handler)
  {
    service_.set_option(impl_, option, error_handler);
  }

  /// Get an option from the acceptor.
  /**
   * This function is used to get the current value of an option on the
   * acceptor.
   *
   * @param option The option value to be obtained from the acceptor.
   *
   * @throws boost::asio::error Thrown on failure.
   *
   * @sa Socket_Option @n
   * boost::asio::socket_base::reuse_address
   *
   * @par Example:
   * Getting the value of the SOL_SOCKET/SO_REUSEADDR option:
   * @code
   * boost::asio::socket_acceptor acceptor(demuxer);
   * ...
   * boost::asio::socket_acceptor::reuse_address option;
   * acceptor.get_option(option);
   * bool is_set = option.get();
   * @endcode
   */
  template <typename Option>
  void get_option(Option& option)
  {
    service_.get_option(impl_, option, throw_error());
  }

  /// Get an option from the acceptor.
  /**
   * This function is used to get the current value of an option on the
   * acceptor.
   *
   * @param option The option value to be obtained from the acceptor.
   *
   * @param error_handler The handler to be called when an error occurs. Copies
   * will be made of the handler as required. The function signature of the
   * handler must be:
   * @code void error_handler(
   *   const boost::asio::error& error // Result of operation
   * ); @endcode
   *
   * @sa Socket_Option @n
   * boost::asio::socket_base::reuse_address
   *
   * @par Example:
   * Getting the value of the SOL_SOCKET/SO_REUSEADDR option:
   * @code
   * boost::asio::socket_acceptor acceptor(demuxer);
   * ...
   * boost::asio::socket_acceptor::reuse_address option;
   * boost::asio::error error;
   * acceptor.get_option(option, boost::asio::assign_error(error));
   * if (error)
   * {
   *   // An error occurred.
   * }
   * bool is_set = option.get();
   * @endcode
   */
  template <typename Option, typename Error_Handler>
  void get_option(Option& option, Error_Handler error_handler)
  {
    service_.get_option(impl_, option, error_handler);
  }

  /// Get the local endpoint of the acceptor.
  /**
   * This function is used to obtain the locally bound endpoint of the
   * acceptor.
   *
   * @param endpoint An endpoint object that receives the local endpoint of the
   * acceptor.
   *
   * @throws boost::asio::error Thrown on failure.
   *
   * @par Example:
   * @code
   * boost::asio::socket_acceptor acceptor(demuxer);
   * ...
   * boost::asio::ipv4::tcp::endpoint endpoint;
   * acceptor.get_local_endpoint(endpoint);
   * @endcode
   */
  template <typename Endpoint>
  void get_local_endpoint(Endpoint& endpoint)
  {
    service_.get_local_endpoint(impl_, endpoint, throw_error());
  }

  /// Get the local endpoint of the acceptor.
  /**
   * This function is used to obtain the locally bound endpoint of the
   * acceptor.
   *
   * @param endpoint An endpoint object that receives the local endpoint of the
   * acceptor.
   *
   * @param error_handler The handler to be called when an error occurs. Copies
   * will be made of the handler as required. The function signature of the
   * handler must be:
   * @code void error_handler(
   *   const boost::asio::error& error // Result of operation
   * ); @endcode
   *
   * @par Example:
   * @code
   * boost::asio::socket_acceptor acceptor(demuxer);
   * ...
   * boost::asio::ipv4::tcp::endpoint endpoint;
   * boost::asio::error error;
   * acceptor.get_local_endpoint(endpoint, boost::asio::assign_error(error));
   * if (error)
   * {
   *   // An error occurred.
   * }
   * @endcode
   */
  template <typename Endpoint, typename Error_Handler>
  void get_local_endpoint(Endpoint& endpoint, Error_Handler error_handler)
  {
    service_.get_local_endpoint(impl_, endpoint, error_handler);
  }

  /// Accept a new connection.
  /**
   * This function is used to accept a new connection from a peer into the
   * given socket. The function call will block until a new connection has been
   * accepted successfully or an error occurs.
   *
   * @param peer The socket into which the new connection will be accepted.
   *
   * @throws boost::asio::error Thrown on failure.
   *
   * @par Example:
   * @code
   * boost::asio::socket_acceptor acceptor(demuxer);
   * ...
   * boost::asio::stream_socket socket;
   * acceptor.accept(socket);
   * @endcode
   */
  template <typename Socket>
  void accept(Socket& peer)
  {
    service_.accept(impl_, to_socket(peer), throw_error());
  }

  /// Accept a new connection.
  /**
   * This function is used to accept a new connection from a peer into the
   * given socket. The function call will block until a new connection has been
   * accepted successfully or an error occurs.
   *
   * @param peer The socket into which the new connection will be accepted.
   *
   * @param error_handler The handler to be called when an error occurs. Copies
   * will be made of the handler as required. The function signature of the
   * handler must be:
   * @code void error_handler(
   *   const boost::asio::error& error // Result of operation
   * ); @endcode
   *
   * @par Example:
   * @code
   * boost::asio::socket_acceptor acceptor(demuxer);
   * ...
   * boost::asio::stream_socket socket;
   * boost::asio::error error;
   * acceptor.accept(socket, boost::asio::assign_error(error));
   * if (error)
   * {
   *   // An error occurred.
   * }
   * @endcode
   */
  template <typename Socket, typename Error_Handler>
  void accept(Socket& peer, Error_Handler error_handler)
  {
    service_.accept(impl_, to_socket(peer), error_handler);
  }

  /// Start an asynchronous accept.
  /**
   * This function is used to asynchronously accept a new connection into a
   * socket. The function call always returns immediately.
   *
   * @param peer The socket into which the new connection will be accepted.
   * Ownership of the peer object is retained by the caller, which must
   * guarantee that it is valid until the handler is called.
   *
   * @param handler The handler to be called when the accept operation
   * completes. Copies will be made of the handler as required. The function
   * signature of the handler must be:
   * @code void handler(
   *   const boost::asio::error& error // Result of operation
   * ); @endcode
   * Regardless of whether the asynchronous operation completes immediately or
   * not, the handler will not be invoked from within this function. Invocation
   * of the handler will be performed in a manner equivalent to using
   * boost::asio::demuxer::post().
   *
   * @par Example:
   * @code
   * void accept_handler(const boost::asio::error& error)
   * {
   *   if (!error)
   *   {
   *     // Accept succeeded.
   *   }
   * }
   *
   * ...
   *
   * boost::asio::socket_acceptor acceptor(demuxer);
   * ...
   * boost::asio::stream_socket socket;
   * acceptor.async_accept(socket, accept_handler);
   * @endcode
   */
  template <typename Socket, typename Handler>
  void async_accept(Socket& peer, Handler handler)
  {
    service_.async_accept(impl_, to_socket(peer), handler);
  }

  /// Accept a new connection and obtain the endpoint of the peer
  /**
   * This function is used to accept a new connection from a peer into the
   * given socket, and additionally provide the endpoint of the remote peer.
   * The function call will block until a new connection has been accepted
   * successfully or an error occurs.
   *
   * @param peer The socket into which the new connection will be accepted.
   *
   * @param peer_endpoint An endpoint object which will receive the endpoint of
   * the remote peer.
   *
   * @throws boost::asio::error Thrown on failure.
   *
   * @par Example:
   * @code
   * boost::asio::socket_acceptor acceptor(demuxer);
   * ...
   * boost::asio::stream_socket socket;
   * boost::asio::ipv4::tcp::endpoint endpoint;
   * acceptor.accept_endpoint(socket, endpoint);
   * @endcode
   */
  template <typename Socket, typename Endpoint>
  void accept_endpoint(Socket& peer, Endpoint& peer_endpoint)
  {
    service_.accept_endpoint(impl_, to_socket(peer), peer_endpoint,
        throw_error());
  }

  /// Accept a new connection and obtain the endpoint of the peer
  /**
   * This function is used to accept a new connection from a peer into the
   * given socket, and additionally provide the endpoint of the remote peer.
   * The function call will block until a new connection has been accepted
   * successfully or an error occurs.
   *
   * @param peer The socket into which the new connection will be accepted.
   *
   * @param peer_endpoint An endpoint object which will receive the endpoint of
   * the remote peer.
   *
   * @param error_handler The handler to be called when an error occurs. Copies
   * will be made of the handler as required. The function signature of the
   * handler must be:
   * @code void error_handler(
   *   const boost::asio::error& error // Result of operation
   * ); @endcode
   *
   * @par Example:
   * @code
   * boost::asio::socket_acceptor acceptor(demuxer);
   * ...
   * boost::asio::stream_socket socket;
   * boost::asio::ipv4::tcp::endpoint endpoint;
   * boost::asio::error error;
   * acceptor.accept_endpoint(socket, endpoint,
   *     boost::asio::assign_error(error));
   * if (error)
   * {
   *   // An error occurred.
   * }
   * @endcode
   */
  template <typename Socket, typename Endpoint, typename Error_Handler>
  void accept_endpoint(Socket& peer, Endpoint& peer_endpoint,
      Error_Handler error_handler)
  {
    service_.accept_endpoint(impl_, to_socket(peer), peer_endpoint,
        error_handler);
  }

  /// Start an asynchronous accept.
  /**
   * This function is used to asynchronously accept a new connection into a
   * socket, and additionally obtain the endpoint of the remote peer. The
   * function call always returns immediately.
   *
   * @param peer The socket into which the new connection will be accepted.
   * Ownership of the peer object is retained by the caller, which must
   * guarantee that it is valid until the handler is called.
   *
   * @param peer_endpoint An endpoint object into which the endpoint of the
   * remote peer will be written. Ownership of the peer_endpoint object is
   * retained by the caller, which must guarantee that it is valid until the
   * handler is called.
   *
   * @param handler The handler to be called when the accept operation
   * completes. Copies will be made of the handler as required. The function
   * signature of the handler must be:
   * @code void handler(
   *   const boost::asio::error& error // Result of operation
   * ); @endcode
   * Regardless of whether the asynchronous operation completes immediately or
   * not, the handler will not be invoked from within this function. Invocation
   * of the handler will be performed in a manner equivalent to using
   * boost::asio::demuxer::post().
   */
  template <typename Socket, typename Endpoint, typename Handler>
  void async_accept_endpoint(Socket& peer, Endpoint& peer_endpoint,
      Handler handler)
  {
    service_.async_accept_endpoint(impl_, to_socket(peer), peer_endpoint,
        handler);
  }

private:
  /// The backend service implementation.
  service_type& service_;

  /// The underlying native implementation.
  impl_type impl_;

  // Helper function to convert a stack of layers into a socket.
  template <typename Socket>
  typename Socket::lowest_layer_type& to_socket(Socket& peer)
  {
    return peer.lowest_layer();
  }

  // Helper class to automatically close the implementation on block exit.
  class close_on_block_exit
  {
  public:
    close_on_block_exit(service_type& service, impl_type& impl)
      : service_(&service), impl_(impl)
    {
    }

    ~close_on_block_exit()
    {
      if (service_)
      {
        service_->close(impl_, ignore_error());
      }
    }

    void cancel()
    {
      service_ = 0;
    }

  private:
    service_type* service_;
    impl_type& impl_;
  };
};

} // namespace asio
} // namespace boost

#include <boost/asio/detail/pop_options.hpp>

#endif // BOOST_ASIO_BASIC_SOCKET_ACCEPTOR_HPP